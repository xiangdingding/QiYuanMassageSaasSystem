using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Tenants;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 按摩店自助注册弹窗：收集店铺信息 + 店主登录手机号/密码，调用公开匿名端点 /tenants/register，
/// 成功后获 30 天试用。注册成功把 <see cref="RegisteredPhone"/> 暴露给登录页回填账号。
/// </summary>
public partial class RegisterWindow : Window
{
    private readonly IApiClient _api;
    private bool _busy;

    /// <summary>注册成功后的店主登录手机号；未成功为 null。</summary>
    public string? RegisteredPhone { get; private set; }

    public RegisterWindow(IApiClient api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;

        var name = NameBox.Text.Trim();
        var ownerPhone = OwnerPhoneBox.Text.Trim();
        var password = PasswordBox.Password;
        var confirm = ConfirmPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(name))
        {
            Fail("请输入店铺名"); return;
        }
        if (!Regex.IsMatch(ownerPhone, @"^\d{11}$"))
        {
            Fail("请输入 11 位登录手机号"); return;
        }
        if (password.Length < 6)
        {
            Fail("登录密码至少 6 位"); return;
        }
        if (password != confirm)
        {
            Fail("两次输入的密码不一致"); return;
        }
        if (AgreeCheck.IsChecked != true)
        {
            Fail("请先阅读并勾选同意《用户服务协议》和《隐私协议》"); return;
        }

        var req = new RegisterTenantRequest(
            Name: name,
            // 与 BS 端一致：登录手机号同时作为店铺联系电话
            ContactPhone: ownerPhone,
            ContactName: string.IsNullOrWhiteSpace(ContactNameBox.Text) ? null : ContactNameBox.Text.Trim(),
            OwnerPhone: ownerPhone,
            OwnerPassword: password,
            OwnerRealName: null);

        try
        {
            SetBusy(true);
            var resp = await _api.RegisterTenantAsync(req);
            RegisteredPhone = resp.OwnerPhone;
            MessageBox.Show(
                $"注册成功！\n\n门店：{resp.TenantName}\n登录手机号：{resp.OwnerPhone}\n试用至：{resp.ExpireAt.ToLocalTime():yyyy-MM-dd}（{resp.TrialDays} 天）\n\n请用该手机号与刚设置的密码登录。",
                "注册成功", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            var (_, msg) = ErrorReporter.Parse(ex);
            Fail(msg);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // 协议正文懒加载并缓存：匿名端点，注册（登录前）即可读取。
    private MassageSaas.Shared.Settings.PlatformAgreementDto? _agreements;

    private async Task<MassageSaas.Shared.Settings.PlatformAgreementDto?> EnsureAgreementsAsync()
    {
        if (_agreements is not null) return _agreements;
        try { _agreements = await _api.GetPlatformAgreementsAsync(); }
        catch (Exception ex) { Fail(ErrorReporter.Parse(ex).Item2); }
        return _agreements;
    }

    private async void ServiceAgreement_Click(object sender, RoutedEventArgs e)
    {
        var a = await EnsureAgreementsAsync();
        if (a is not null)
            new TextViewWindow("用户服务协议", a.ServiceAgreement) { Owner = this }.ShowDialog();
    }

    private async void PrivacyPolicy_Click(object sender, RoutedEventArgs e)
    {
        var a = await EnsureAgreementsAsync();
        if (a is not null)
            new TextViewWindow("隐私协议", a.PrivacyPolicy) { Owner = this }.ShowDialog();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void Fail(string msg) => ErrorText.Text = msg;

    private void SetBusy(bool busy)
    {
        _busy = busy;
        SubmitButton.IsEnabled = !busy;
        SubmitButton.Content = busy ? "注册中…" : "注 册";
    }
}
