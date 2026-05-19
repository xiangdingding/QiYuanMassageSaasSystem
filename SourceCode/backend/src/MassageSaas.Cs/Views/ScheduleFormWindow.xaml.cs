using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using MassageSaas.Shared.Schedules;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.Views;

public partial class ScheduleFormWindow : Window
{
    /// <summary>技师下拉项：显示友好名，提交时用 Id。</summary>
    public record StaffPick(long Id, string Name);

    private readonly long _storeId;

    public ScheduleFormWindow(long storeId, IEnumerable<StaffDto> staff)
    {
        InitializeComponent();
        _storeId = storeId;
        UserBox.ItemsSource = staff
            .Select(s => new StaffPick(s.Id, s.RealName ?? s.Username))
            .ToList();
        if (UserBox.Items.Count > 0) UserBox.SelectedIndex = 0;
        DateBox.SelectedDate = System.DateTime.Today;
    }

    private static bool IsValidTime(string s) =>
        Regex.IsMatch(s.Trim(), @"^([01]?\d|2[0-3]):[0-5]\d$");

    public CreateStaffScheduleRequest BuildRequest()
    {
        var pick = (StaffPick)UserBox.SelectedItem;
        return new CreateStaffScheduleRequest(
            StoreId: _storeId,
            UserId: pick.Id,
            WorkDate: DateBox.SelectedDate!.Value.Date,
            StartTime: StartBox.Text.Trim(),
            EndTime: EndBox.Text.Trim(),
            Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim());
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (UserBox.SelectedItem is null) { MessageBox.Show("请选择技师"); return; }
        if (DateBox.SelectedDate is null) { MessageBox.Show("请选择日期"); return; }
        if (!IsValidTime(StartBox.Text) || !IsValidTime(EndBox.Text))
        {
            MessageBox.Show("时间格式应为 HH:mm，如 09:00");
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
