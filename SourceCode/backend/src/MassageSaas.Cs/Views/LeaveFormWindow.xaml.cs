using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MassageSaas.Shared.Schedules;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.Views;

/// <summary>登记请假：为员工提交请假单（类型 + 起止日期 + 事由），逻辑与 BS 端 submitLeave 一致。</summary>
public partial class LeaveFormWindow : Window
{
    /// <summary>员工下拉项：显示「姓名（角色）」，提交时用 Id。</summary>
    public record StaffPick(long Id, string Display)
    {
        public override string ToString() => Display;
    }

    public LeaveFormWindow(IEnumerable<StaffDto> staff)
    {
        InitializeComponent();
        UserBox.ItemsSource = staff
            .Select(s => new StaffPick(s.Id, $"{s.RealName ?? s.Username}（{RoleCn(s.Role)}）"))
            .ToList();
        if (UserBox.Items.Count > 0) UserBox.SelectedIndex = 0;
        FromBox.SelectedDate = DateTime.Today;
        ToBox.SelectedDate = DateTime.Today;
        UpdateDays();
    }

    /// <summary>取该日期当天中午（避开服务端北京时间-8h 把 00:00 减成前一天）。</summary>
    private static DateTime NoonOf(DateTime d) => new(d.Year, d.Month, d.Day, 12, 0, 0);

    private string SelectedStartHalf() => (StartHalfBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "Morning";
    private string SelectedEndHalf() => (EndHalfBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "Afternoon";

    /// <summary>折算天数（上午=整天边界，下午起首日扣0.5，上午止末日扣0.5）。</summary>
    private decimal ComputeDays()
    {
        if (FromBox.SelectedDate is not DateTime f || ToBox.SelectedDate is not DateTime t) return 0m;
        if (t.Date < f.Date) return 0m;
        decimal days = (t.Date - f.Date).Days + 1;
        if (SelectedStartHalf() == "Afternoon") days -= 0.5m;
        if (SelectedEndHalf() == "Morning") days -= 0.5m;
        return days < 0m ? 0m : days;
    }

    private void UpdateDays()
    {
        if (DaysText is null) return;
        var d = ComputeDays();
        DaysText.Text = d <= 0m ? "时长无效（同日不能从下午请到上午）" : $"共 {d:0.#} 天";
    }

    private void OnInputChanged(object sender, RoutedEventArgs e) => UpdateDays();

    private static string RoleCn(string role) => role switch
    {
        "ShopOwner" => "店主",
        "StoreManager" => "店长",
        "Cashier" => "收银员",
        "Technician" => "技师",
        _ => role
    };

    private string SelectedType() =>
        (TypeBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "Personal";

    public CreateLeaveRequest BuildRequest()
    {
        var pick = (StaffPick)UserBox.SelectedItem;
        return new CreateLeaveRequest(
            UserId: pick.Id,
            Type: SelectedType(),
            // 取当天中午：服务端 BeijingDateTimeConverter 会把无偏移 00:00 按北京时间-8h 存成前一天，
            // 用 12:00 减 8h 仍是当天，保证起止日期就是用户选的那天。
            FromDate: NoonOf(FromBox.SelectedDate!.Value),
            ToDate: NoonOf(ToBox.SelectedDate!.Value),
            Reason: string.IsNullOrWhiteSpace(ReasonBox.Text) ? null : ReasonBox.Text.Trim(),
            StartHalf: SelectedStartHalf(),
            EndHalf: SelectedEndHalf());
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (UserBox.SelectedItem is null) { MessageBox.Show("请选择员工"); return; }
        if (FromBox.SelectedDate is null || ToBox.SelectedDate is null) { MessageBox.Show("请选择起止日期"); return; }
        if (ToBox.SelectedDate < FromBox.SelectedDate) { MessageBox.Show("结束日期不能早于开始日期"); return; }
        if (ComputeDays() <= 0m) { MessageBox.Show("请假时长须大于 0（同日不能从下午请到上午）"); return; }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    /// <summary>点击日期文本区域即展开日历。</summary>
    private void DatePicker_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DatePicker dp) return;
        if (e.OriginalSource is DependencyObject d && FindAncestor<ButtonBase>(d) is not null) return;
        if (dp.IsDropDownOpen) return;
        dp.Dispatcher.BeginInvoke(new Action(() => dp.IsDropDownOpen = true), DispatcherPriority.Input);
    }

    private static T? FindAncestor<T>(DependencyObject? node) where T : DependencyObject
    {
        while (node is not null)
        {
            if (node is T t) return t;
            node = VisualTreeHelper.GetParent(node);
        }
        return null;
    }
}
