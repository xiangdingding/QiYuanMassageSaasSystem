using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Payroll;

namespace MassageSaas.Cs.Views;

public partial class PayrollItemEditWindow : Window
{
    public PayrollItemEditWindow(PayrollItemDto item)
    {
        InitializeComponent();
        UserLabel.Text = $"员工：{item.UserName}";
        OvertimeBox.Text = item.OvertimeHours.ToString("0.##", CultureInfo.InvariantCulture);
        RemarkBox.Text = item.Remark ?? string.Empty;
    }

    private decimal ParseDec(TextBox b, decimal fallback)
        => decimal.TryParse(b.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    public UpdatePayrollItemRequest BuildRequest() => new(
        OvertimeHours: ParseDec(OvertimeBox, 0m),
        AttendanceBonusOverride: ParseDec(BonusBox, -1m),
        Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim());

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (ParseDec(OvertimeBox, -1m) < 0)
        {
            MessageBox.Show("加班小时数不能为负");
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
