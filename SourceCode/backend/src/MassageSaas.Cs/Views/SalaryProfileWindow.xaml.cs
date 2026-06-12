using System.Windows;
using MassageSaas.Shared.Payroll;

namespace MassageSaas.Cs.Views;

public partial class SalaryProfileWindow : Window
{
    public SalaryProfileWindow(SalaryProfileDto profile)
    {
        InitializeComponent();
        UserLabel.Text = $"员工：{profile.UserName}";
        BaseBox.Value = (double)profile.BaseMonthly;
        OtRateBox.Value = (double)profile.OvertimeHourRate;
        BonusBox.Value = (double)profile.AttendanceBonusAmount;
        DaysBox.Value = profile.RequiredAttendanceDays;
        RemarkBox.Text = profile.Remark ?? string.Empty;
    }

    // 数值由 NumericUpDown 约束（Minimum=0），无需再做非负校验
    public UpsertSalaryProfileRequest BuildRequest() => new(
        BaseMonthly: (decimal)BaseBox.Value,
        OvertimeHourRate: (decimal)OtRateBox.Value,
        AttendanceBonusAmount: (decimal)BonusBox.Value,
        RequiredAttendanceDays: (int)DaysBox.Value,
        Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim());

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
