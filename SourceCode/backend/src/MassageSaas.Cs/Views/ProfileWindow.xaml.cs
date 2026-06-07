using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Auth;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 个人设置（对齐 BS ProfileDialog）：基本资料（账号 / 角色只读，姓名 / 手机号可改）+ 修改密码。
/// 改资料后同步会话展示名；改密码成功需重新登录（<see cref="PasswordChanged"/> 通知调用方登出）。
/// </summary>
public partial class ProfileWindow : Window
{
    private readonly IApiClient _api;
    private readonly SessionService _session;
    private string? _originalPhone;
    private bool _busy;

    /// <summary>密码已修改：调用方据此触发重新登录。</summary>
    public bool PasswordChanged { get; private set; }

    private static readonly Dictionary<string, string> RoleLabels = new()
    {
        ["PlatformAdmin"] = "平台管理员",
        ["ShopOwner"] = "店主",
        ["StoreManager"] = "店长",
        ["Cashier"] = "收银员",
        ["Technician"] = "技师"
    };

    public ProfileWindow(IApiClient api, SessionService session)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var p = await _api.GetProfileAsync();
            UsernameBox.Text = p.Username;
            RoleBox.Text = RoleLabels.TryGetValue(p.Role, out var label) ? label : p.Role;
            RealNameBox.Text = p.RealName ?? string.Empty;
            PhoneBox.Text = p.Phone ?? string.Empty;
            _originalPhone = p.Phone;
        }
        catch (Exception ex)
        {
            var (_, msg) = ErrorReporter.Parse(ex);
            Fail(msg);
        }
    }

    private async void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        Fail(null);

        var phone = PhoneBox.Text.Trim();
        if (!string.IsNullOrEmpty(phone) && !Regex.IsMatch(phone, @"^\d{11}$"))
        {
            Fail("请输入 11 位手机号"); return;
        }

        var realName = string.IsNullOrWhiteSpace(RealNameBox.Text) ? null : RealNameBox.Text.Trim();
        try
        {
            SetBusy(true);
            var updated = await _api.UpdateProfileAsync(new UpdateProfileRequest(realName, string.IsNullOrEmpty(phone) ? null : phone));
            _session.UpdateRealName(updated.RealName);

            var phoneChanged = (string.IsNullOrEmpty(phone) ? null : phone) != _originalPhone;
            _originalPhone = updated.Phone;
            if (phoneChanged)
                MessageBox.Show("手机号已修改，下次登录请使用新手机号。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            var (_, msg) = ErrorReporter.Parse(ex);
            Fail(msg);
        }
        finally { SetBusy(false); }
    }

    private async void SavePassword_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        Fail(null);

        var oldPwd = OldPwdBox.Password;
        var newPwd = NewPwdBox.Password;
        var confirm = ConfirmPwdBox.Password;

        if (string.IsNullOrEmpty(oldPwd)) { Fail("请输入原密码"); return; }
        if (newPwd.Length < 6) { Fail("新密码至少 6 位"); return; }
        if (newPwd != confirm) { Fail("两次输入的新密码不一致"); return; }

        try
        {
            SetBusy(true);
            await _api.ChangePasswordAsync(new ChangePasswordRequest(oldPwd, newPwd));
            PasswordChanged = true;
            MessageBox.Show("密码修改成功，请重新登录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            var (_, msg) = ErrorReporter.Parse(ex);
            Fail(msg);
        }
        finally { SetBusy(false); }
    }

    private void Fail(string? msg) => ErrorText.Text = msg ?? string.Empty;

    private void SetBusy(bool busy)
    {
        _busy = busy;
        SaveProfileButton.IsEnabled = !busy;
        SavePwdButton.IsEnabled = !busy;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }
}
