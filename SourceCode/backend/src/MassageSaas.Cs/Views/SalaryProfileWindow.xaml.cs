using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Payroll;

namespace MassageSaas.Cs.Views;

public partial class SalaryProfileWindow : Window
{
    public SalaryProfileWindow(SalaryProfileDto profile)
    {
        InitializeComponent();
        UserLabel.Text = $"员工：{profile.UserName}";
        BaseBox.Text = profile.BaseMonthly.ToString("0.##", CultureInfo.InvariantCulture);
        OtRateBox.Text = profile.OvertimeHourRate.ToString("0.##", CultureInfo.InvariantCulture);
        BonusBox.Text = profile.AttendanceBonusAmount.ToString("0.##", CultureInfo.InvariantCulture);
        DaysBox.Text = profile.RequiredAttendanceDays.ToString(CultureInfo.InvariantCulture);
        RemarkBox.Text = profile.Remark ?? string.Empty;
    }

    private decimal ParseDec(TextBox b, decimal fallback)
        => decimal.TryParse(b.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : fallback;
    private int ParseInt(TextBox b, int fallback)
        => int.TryParse(b.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    public UpsertSalaryProfileRequest BuildRequest() => new(
        BaseMonthly: ParseDec(BaseBox, 0m),
        OvertimeHourRate: ParseDec(OtRateBox, 0m),
        AttendanceBonusAmount: ParseDec(BonusBox, 0m),
        RequiredAttendanceDays: ParseInt(DaysBox, 0),
        Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim());

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (ParseDec(BaseBox, -1m) < 0 || ParseDec(OtRateBox, -1m) < 0
            || ParseDec(BonusBox, -1m) < 0 || ParseInt(DaysBox, -1) < 0)
        {
            MessageBox.Show("金额与天数不能为负");
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
