using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MassageSaas.Shared.Schedules;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.Views;

public partial class ScheduleFormWindow : Window
{
    /// <summary>员工下拉项：显示「姓名（角色）」，提交时用 Id。</summary>
    public record StaffPick(long Id, string Display)
    {
        // 自定义下拉框模板下选中区会回退到 ToString，避免显示记录默认的 JSON 形式
        public override string ToString() => Display;
    }

    private readonly long _storeId;

    /// <summary>下拉时间选项：每 30 分钟一档，整天可选；可编辑下拉也允许手动输入任意 HH:mm。</summary>
    private static readonly List<string> TimeOptions = BuildTimeOptions();

    private static List<string> BuildTimeOptions()
    {
        var list = new List<string>();
        for (var h = 0; h < 24; h++)
            for (var m = 0; m < 60; m += 30)
                list.Add($"{h:D2}:{m:D2}");
        return list;
    }

    public ScheduleFormWindow(long storeId, IEnumerable<StaffDto> staff)
    {
        InitializeComponent();
        _storeId = storeId;
        UserBox.ItemsSource = staff
            .Select(s => new StaffPick(s.Id, $"{s.RealName ?? s.Username}（{RoleCn(s.Role)}）"))
            .ToList();
        if (UserBox.Items.Count > 0) UserBox.SelectedIndex = 0;
        DateBox.SelectedDate = DateTime.Today;

        StartBox.ItemsSource = TimeOptions;
        EndBox.ItemsSource = TimeOptions;
        StartBox.Text = "09:00";
        EndBox.Text = "21:00";
    }

    private static string RoleCn(string role) => role switch
    {
        "ShopOwner" => "店主",
        "StoreManager" => "店长",
        "Cashier" => "收银员",
        "Technician" => "技师",
        _ => role
    };

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

    private static bool IsValidTime(string s) =>
        Regex.IsMatch(s.Trim(), @"^([01]?\d|2[0-3]):[0-5]\d$");

    /// <summary>取该日期当天中午（避开服务端北京时间-8h 把 00:00 减成前一天）。</summary>
    private static DateTime NoonOf(DateTime d) => new(d.Year, d.Month, d.Day, 12, 0, 0);

    public CreateStaffScheduleRequest BuildRequest()
    {
        var pick = (StaffPick)UserBox.SelectedItem;
        return new CreateStaffScheduleRequest(
            StoreId: _storeId,
            UserId: pick.Id,
            // 纯日期取当天中午：服务端 BeijingDateTimeConverter 会把无偏移时间按北京时间-8h 存 UTC，
            // 用 00:00 会被减成前一天，用 12:00 减 8h 仍是当天，保证存的就是用户选的那天。
            WorkDate: NoonOf(DateBox.SelectedDate!.Value),
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
