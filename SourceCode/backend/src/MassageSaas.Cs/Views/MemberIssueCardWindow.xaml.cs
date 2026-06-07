using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Members;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 办卡：给一张已有的会员卡按会员类型追加——充值卡加余额(含赠送)，计次卡发放一个计次套餐。
/// 调用后端 POST /members/{id}/issue-card。逻辑与 BS 端 openIssueCard / doIssueCard 一致。
/// </summary>
public partial class MemberIssueCardWindow : Window
{
    private readonly IApiClient _api;
    private readonly MemberDto _member;

    public MemberIssueCardWindow(IApiClient api, MemberDto member)
    {
        InitializeComponent();
        _api = api;
        _member = member;
        HeaderText.Text = $"办卡：{member.Name ?? member.CardNo}（{member.CardNo}）";
        _ = LoadAsync();
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        try
        {
            var types = await _api.GetMemberTypesAsync(includeInactive: false);
            TypeBox.ItemsSource = types.Where(t => t.IsActive).ToList();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    private MemberTypeDto? SelectedType => TypeBox.SelectedItem as MemberTypeDto;
    private bool IsCountBased => SelectedType?.Kind == "CountBased";

    private void Type_Changed(object sender, SelectionChangedEventArgs e)
    {
        var t = SelectedType;
        if (t is null) { RulesBox.Visibility = Visibility.Collapsed; return; }

        var threshold = t.Kind == "StoredValue"
            ? $"最低充值 ¥{t.MinRechargeAmount ?? 0m:F2}"
            : $"最少 {t.MinPurchaseCount ?? 1} 次（绑定 {t.ServiceItemName}）";
        var discount = t.Discount < 1m ? $" · {t.Discount * 10m:0.#} 折" : "";
        var bonus = t.Kind == "StoredValue"
            ? ((t.BonusAmount ?? 0m) > 0 ? $" · 送 ¥{t.BonusAmount:F2}" : "")
            : ((t.BonusCount ?? 0) > 0 ? $" · 送 {t.BonusCount} 次" : "");
        var valid = t.ValidDays is int d && d > 0 ? $" · 有效 {d} 天" : " · 永久有效";
        RulesText.Text = $"{t.Name}：{threshold}{discount}{bonus}{valid}";
        RulesBox.Visibility = Visibility.Visible;

        StoredPanel.Visibility = IsCountBased ? Visibility.Collapsed : Visibility.Visible;
        CountPanel.Visibility = IsCountBased ? Visibility.Visible : Visibility.Collapsed;
        if (IsCountBased)
            CountBox.Text = (t.MinPurchaseCount ?? 1).ToString(CultureInfo.InvariantCulture);
        else
            AmountBox.Text = (t.MinRechargeAmount ?? 0m).ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static int ParseInt(string? s) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
    private static decimal ParseDec(string? s) =>
        decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;

    private string PayMethod()
    {
        foreach (var c in PayPanel.Children)
            if (c is RadioButton { IsChecked: true } rb && rb.Tag is string tag) return tag;
        return "Wechat";
    }

    private async void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var t = SelectedType;
        if (t is null) { Warn("请选择会员类型"); return; }

        decimal amount;
        int count;
        if (IsCountBased)
        {
            count = ParseInt(CountBox.Text);
            if (count < (t.MinPurchaseCount ?? 1)) { Warn($"购买次数不能低于 {t.MinPurchaseCount ?? 1} 次"); return; }
            amount = ParseDec(PaidBox.Text); // 实收现金价，自由填
        }
        else
        {
            amount = ParseDec(AmountBox.Text);
            count = 0;
            if (amount < (t.MinRechargeAmount ?? 0m)) { Warn($"充值金额不能低于 ¥{t.MinRechargeAmount ?? 0m:F2}"); return; }
        }

        var remark = string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim();
        try
        {
            ConfirmButton.IsEnabled = false;
            var r = await _api.IssueCardAsync(_member.Id,
                new IssueCardRequest(t.Id, amount, count, PayMethod(), remark));
            var msg = t.Kind == "StoredValue"
                ? $"办卡成功，余额到账 ¥{r.NewBalance:F2}（含赠送 ¥{r.BonusAmount:F2}）"
                : $"办卡成功，已发放 {count + r.BonusCount} 次「{t.ServiceItemName}」（含赠送 {r.BonusCount} 次）";
            MessageBox.Show(msg, "办卡", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorReporter.Show(ex);
            ConfirmButton.IsEnabled = true;
        }
    }

    private static void Warn(string msg) =>
        MessageBox.Show(msg, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
