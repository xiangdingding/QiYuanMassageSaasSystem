using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Members;

namespace MassageSaas.Cs.Views;

public partial class RechargeWindow : Window
{
    public decimal Amount { get; private set; }
    public decimal BonusAmount { get; private set; }
    public string PayMethod { get; private set; } = "Cash";
    public string? Remark { get; private set; }

    public RechargeWindow(MemberDto member)
    {
        InitializeComponent();
        HeaderText.Text = $"{member.Name ?? member.CardNo}  当前余额 ¥{member.Balance:F2}";
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(AmountBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var a) || a <= 0)
        {
            MessageBox.Show("充值金额必须 > 0");
            return;
        }
        decimal.TryParse(BonusBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var b);
        if (b < 0) b = 0;

        Amount = a;
        BonusAmount = b;
        PayMethod = (PayBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "Cash";
        Remark = string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
