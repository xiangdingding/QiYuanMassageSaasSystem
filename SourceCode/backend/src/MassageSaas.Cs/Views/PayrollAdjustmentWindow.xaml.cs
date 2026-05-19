using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Payroll;

namespace MassageSaas.Cs.Views;

public partial class PayrollAdjustmentWindow : Window
{
    public PayrollAdjustmentWindow(string userName)
    {
        InitializeComponent();
        UserLabel.Text = $"员工：{userName}";
        KindBox.SelectedIndex = 0;
    }

    private string SelectedKind() =>
        (KindBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "Bonus";

    private decimal ParseAmount()
        => decimal.TryParse(AmountBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;

    public AddAdjustmentRequest BuildRequest() => new(
        Kind: SelectedKind(),
        Amount: ParseAmount(),
        Reason: ReasonBox.Text.Trim());

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (ParseAmount() <= 0)
        {
            MessageBox.Show("金额必须大于 0");
            return;
        }
        if (string.IsNullOrWhiteSpace(ReasonBox.Text))
        {
            MessageBox.Show("请填写奖扣原因");
            return;
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
