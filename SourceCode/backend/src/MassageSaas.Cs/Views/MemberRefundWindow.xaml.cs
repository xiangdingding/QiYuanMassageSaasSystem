using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Members;

namespace MassageSaas.Cs.Views;

/// <summary>退卡：把会员剩余余额按指定方式退还，并关闭该卡。逻辑与 BS 端 doRefund 一致。</summary>
public partial class MemberRefundWindow : Window
{
    private readonly decimal _balance;

    public MemberRefundWindow(MemberDto m)
    {
        InitializeComponent();
        _balance = m.Balance;
        HeaderText.Text = $"退卡：{m.Name ?? m.CardNo}（{m.CardNo}）";
        BalanceText.Text = $"¥{m.Balance:F2}";
        AmountBox.Text = m.Balance.ToString("0.##", CultureInfo.InvariantCulture); // 默认全额
    }

    public decimal RefundAmount { get; private set; }
    public string RefundMethod { get; private set; } = "Wechat";
    public string? Reason { get; private set; }

    private void RefundAll_Click(object sender, RoutedEventArgs e) =>
        AmountBox.Text = _balance.ToString("0.##", CultureInfo.InvariantCulture);

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(AmountBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            MessageBox.Show("退款金额必须大于 0", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (amount > _balance)
        {
            MessageBox.Show($"退款金额不能超过当前余额 ¥{_balance:F2}", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        RefundAmount = amount;
        RefundMethod = CheckedMethod();
        Reason = string.IsNullOrWhiteSpace(ReasonBox.Text) ? null : ReasonBox.Text.Trim();
        DialogResult = true;
        Close();
    }

    private string CheckedMethod()
    {
        foreach (var child in MethodPanel.Children)
            if (child is RadioButton { IsChecked: true } rb && rb.Tag is string tag)
                return tag;
        return "Wechat";
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
