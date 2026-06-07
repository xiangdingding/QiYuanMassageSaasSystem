using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Members;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 会员充值（对齐 BS 端 openRecharge / doRecharge）：
/// - 卡有会员类型(memberTypeId)：走 issueCard，按类型规则——充值卡按金额、计次卡按次数，自动赠送/折扣；
/// - 卡无会员类型(旧卡)：走旧的 recharge，自由填充值金额 + 赠送金额。
/// 窗口自行调用接口并在成功后 DialogResult=true，由调用方刷新列表。
/// </summary>
public partial class RechargeWindow : Window
{
    private readonly IApiClient _api;
    private readonly MemberDto _member;
    private MemberTypeDto? _type;
    private decimal _unitPrice;   // 计次卡绑定服务的会员价（会员价>0 优先，否则标准价）

    public RechargeWindow(IApiClient api, MemberDto member)
    {
        InitializeComponent();
        _api = api;
        _member = member;
        HeaderText.Text = $"充值：{member.CardNo}";
        BalanceText.Text = $"当前余额 ¥{member.Balance:F2}";
        AmountBox.ValueChanged += (_, _) => RefreshCalc();
        CountBox.ValueChanged += (_, _) => RefreshCalc();
        _ = LoadAsync();
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        try
        {
            if (_member.MemberTypeId is long tid)
            {
                var services = await _api.GetServicesAsync(false);
                var types = await _api.GetMemberTypesAsync(includeInactive: false);
                _type = types.FirstOrDefault(t => t.Id == tid);
                if (_type?.ServiceItemId is long sid)
                {
                    var s = services.FirstOrDefault(x => x.Id == sid);
                    if (s is not null) _unitPrice = s.MemberPrice > 0 ? s.MemberPrice : s.Price;
                }
            }
            ConfigureUi();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    private void ConfigureUi()
    {
        var t = _type;
        if (t is null)
        {
            // 无模板旧卡：充值金额 + 可编辑赠送，走旧 recharge（无实收/实充推算行）
            RulesBox.Visibility = Visibility.Collapsed;
            StoredPanel.Visibility = Visibility.Visible;
            CountPanel.Visibility = Visibility.Collapsed;
            PaidRow.Visibility = Visibility.Collapsed;
            CreditRow.Visibility = Visibility.Collapsed;
            BonusBox.IsEnabled = true;
            AmountHint.Text = string.Empty;
            BonusHint.Text = string.Empty;
            AmountBox.Minimum = 0;
            AmountBox.Value = 100;
            BonusBox.Value = 0;
            return;
        }

        var kindText = t.Kind == "StoredValue" ? "充值卡" : "计次卡";
        var discount = t.Discount < 1m ? $" · 折扣 {t.Discount * 10m:0.0} 折" : "";
        RulesTitle.Text = $"{t.Name}（{kindText}）";
        RulesBox.Visibility = Visibility.Visible;

        if (t.Kind == "CountBased")
        {
            var bonusTxt = (t.BonusCount ?? 0) > 0 ? $" · 送 {t.BonusCount} 次" : "";
            RulesText.Text = $"最少 {t.MinPurchaseCount ?? 1} 次（绑定：{t.ServiceItemName}）{discount}{bonusTxt}";
            StoredPanel.Visibility = Visibility.Collapsed;
            CountPanel.Visibility = Visibility.Visible;
            CountBox.Minimum = t.MinPurchaseCount ?? 1;
            CountBox.Value = t.MinPurchaseCount ?? 1;
            CountHint.Text = $"最低 {t.MinPurchaseCount ?? 1} 次" + ((t.BonusCount ?? 0) > 0 ? $" · 赠送 {t.BonusCount} 次" : "");
        }
        else
        {
            var bonusTxt = (t.BonusAmount ?? 0m) > 0 ? $" · 送 ¥{t.BonusAmount:F2}" : "";
            RulesText.Text = $"最低充值 ¥{t.MinRechargeAmount ?? 0m:F2}{discount}{bonusTxt}";
            StoredPanel.Visibility = Visibility.Visible;
            CountPanel.Visibility = Visibility.Collapsed;
            PaidRow.Visibility = Visibility.Visible;
            CreditRow.Visibility = Visibility.Visible;
            BonusBox.IsEnabled = false;                 // 赠送由会员类型决定
            BonusHint.Text = "由会员类型决定";
            AmountHint.Text = $"最低 ¥{t.MinRechargeAmount ?? 0m:F2}";
            AmountBox.Minimum = (double)(t.MinRechargeAmount ?? 0m);
            AmountBox.Value = (double)(t.MinRechargeAmount ?? 0m);
            BonusBox.Value = (double)(t.BonusAmount ?? 0m);
        }
        RefreshCalc();
    }

    private void RefreshCalc()
    {
        var t = _type;
        if (t is null) return;   // 旧卡无推算行
        var disc = t.Discount;
        if (t.Kind == "CountBased")
        {
            var count = (int)CountBox.Value;
            var face = Math.Round(count * _unitPrice, 2);
            var paid = Math.Round(face * disc, 2);
            var credit = count + (t.BonusCount ?? 0);
            CountCreditValue.Text = $"{credit} 次";
            CountCreditHint.Text = $"充值次数 + 赠送 {t.BonusCount ?? 0} 次 = 卡内可用次数";
            CountFaceValue.Text = $"¥{face:F2}";
            CountFaceHint.Text = _unitPrice > 0 ? $"{count} 次 × 会员价 ¥{_unitPrice:F2}（{t.ServiceItemName}）" : "绑定服务未配置会员价";
            CountPaidValue.Text = $"¥{paid:F2}";
            CountPaidHint.Text = $"充值金额 × {disc * 10m:0.0} 折 = 客户实付";
        }
        else
        {
            var amount = (decimal)AmountBox.Value;
            var paid = Math.Round(amount * disc, 2);
            var credit = Math.Round(amount + (t.BonusAmount ?? 0m), 2);
            PaidValue.Text = $"¥{paid:F2}";
            PaidHint.Text = $"充值金额 × {disc * 10m:0.0} 折 = 客户实付";
            CreditValue.Text = $"¥{credit:F2}";
            CreditHint.Text = $"充值金额 + 赠送 ¥{t.BonusAmount ?? 0m:F2} = 卡内余额";
        }
    }

    private string PayMethod()
    {
        foreach (var c in PayPanel.Children)
            if (c is RadioButton { IsChecked: true } rb && rb.CommandParameter is string tag) return tag;
        return "Wechat";
    }

    private async void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (!_member.IsActive) { Warn("该卡已退卡 / 关闭，不能再充值"); return; }
        var remark = string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim();
        var pay = PayMethod();
        try
        {
            ConfirmButton.IsEnabled = false;
            if (_type is { } t)
            {
                // 有模板：走 issueCard（后端按类型规则校验 + 自动赠送）
                if (t.Kind == "StoredValue")
                {
                    var amount = (decimal)AmountBox.Value;
                    if (amount < (t.MinRechargeAmount ?? 0m)) { Warn($"充值金额不能低于 ¥{t.MinRechargeAmount ?? 0m:F2}"); ConfirmButton.IsEnabled = true; return; }
                    var r = await _api.IssueCardAsync(_member.Id, new IssueCardRequest(t.Id, amount, 0, pay, remark));
                    MessageBox.Show($"充值成功，余额到账 ¥{r.NewBalance:F2}（含赠送 ¥{r.BonusAmount:F2}）", "充值", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var count = (int)CountBox.Value;
                    if (count < (t.MinPurchaseCount ?? 1)) { Warn($"充值次数不能低于 {t.MinPurchaseCount ?? 1} 次"); ConfirmButton.IsEnabled = true; return; }
                    // 实收 = 次数 × 会员价 × 折扣（两次四舍五入，对齐 BS）
                    var face = Math.Round(count * _unitPrice, 2);
                    var cash = Math.Round(face * t.Discount, 2);
                    var r = await _api.IssueCardAsync(_member.Id, new IssueCardRequest(t.Id, cash, count, pay, remark));
                    MessageBox.Show($"充值成功，已发放 {count + r.BonusCount} 次「{t.ServiceItemName}」（含赠送 {r.BonusCount} 次）", "充值", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                // 无模板旧卡：走旧 recharge
                var amount = (decimal)AmountBox.Value;
                var bonusAmount = (decimal)BonusBox.Value;
                if (amount <= 0) { Warn("充值金额必须大于 0"); ConfirmButton.IsEnabled = true; return; }
                await _api.RechargeAsync(new RechargeRequest(_member.Id, amount, bonusAmount, pay, remark));
                MessageBox.Show($"充值成功，到账 ¥{amount + bonusAmount:F2}", "充值", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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
