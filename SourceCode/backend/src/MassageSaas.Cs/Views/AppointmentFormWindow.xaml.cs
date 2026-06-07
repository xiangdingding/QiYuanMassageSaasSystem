using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Appointments;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 登记电话预约 / 修改预约 / 再次预约（基于已取消单）三合一表单。
/// 逻辑与 BS 端 openCreate / openEdit / openRebook / submitCreate 一致：
/// 修改走 PUT，其余走 POST；再次预约照搬客人信息、时间默认 30 分钟后。
/// </summary>
public partial class AppointmentFormWindow : Window
{
    private readonly IApiClient _api;
    private readonly long _storeId;
    private readonly AppointmentDto? _prefill;
    private readonly bool _isEdit;

    /// <summary>下拉项：Id 为 null 表示"（不指定）"。</summary>
    private record Opt(long? Id, string Label);

    public AppointmentFormWindow(IApiClient api, long storeId, AppointmentDto? prefill, bool isEdit)
    {
        InitializeComponent();
        _api = api;
        _storeId = storeId;
        _prefill = prefill;
        _isEdit = isEdit;

        Title = isEdit ? "修改预约信息" : prefill is not null ? "再次预约" : "登记电话预约";
        HeaderText.Text = isEdit ? "修改预约信息"
            : prefill is not null ? "再次预约（基于已取消单）" : "登记电话预约";
        SaveButton.Content = isEdit ? "保存修改" : "登记";

        // 修改保留原到店时间；新建/再次预约默认 30 分钟后
        var when = isEdit && prefill is not null ? prefill.ExpectedArriveAt : System.DateTime.Now.AddMinutes(30);
        DateBox.SelectedDate = when.Date;
        // 时/分用下拉选择（00-23 / 00-59）
        HourBox.ItemsSource = Enumerable.Range(0, 24).Select(h => h.ToString("D2", CultureInfo.InvariantCulture)).ToList();
        MinuteBox.ItemsSource = Enumerable.Range(0, 60).Select(m => m.ToString("D2", CultureInfo.InvariantCulture)).ToList();
        HourBox.SelectedItem = when.Hour.ToString("D2", CultureInfo.InvariantCulture);
        MinuteBox.SelectedItem = when.Minute.ToString("D2", CultureInfo.InvariantCulture);

        if (prefill is not null)
        {
            NameBox.Text = prefill.CustomerName;
            PhoneBox.Text = prefill.CustomerPhone;
            PartyBox.Text = (prefill.PartySize <= 0 ? 1 : prefill.PartySize).ToString(CultureInfo.InvariantCulture);
            RemarkBox.Text = prefill.Remark ?? string.Empty;
        }

        _ = LoadLookupsAsync();
    }

    private async System.Threading.Tasks.Task LoadLookupsAsync()
    {
        try
        {
            var services = await _api.GetServicesAsync(false);
            var techs = await _api.GetStaffAsync(role: "Technician", pageSize: 200, storeId: _storeId);

            var svcOpts = new List<Opt> { new(null, "（不指定）") };
            svcOpts.AddRange(services.Select(s => new Opt(s.Id, $"{s.Name}（{s.DurationMinutes} 分钟）")));
            ServiceBox.ItemsSource = svcOpts;
            ServiceBox.SelectedValue = _prefill?.ServiceId; // null → 选中"（不指定）"

            var techOpts = new List<Opt> { new(null, "（不指定）") };
            techOpts.AddRange(techs.Items.Select(t => new Opt(t.Id, $"{(t.EmployeeNo?.ToString() ?? "-")} · {t.RealName ?? t.Username}")));
            TechBox.ItemsSource = techOpts;
            TechBox.SelectedValue = _prefill?.PreferredTechnicianId;
        }
        catch (System.Exception ex) { ErrorReporter.Show(ex); }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        var phone = PhoneBox.Text.Trim();
        if (string.IsNullOrEmpty(name)) { Warn("请填写客户姓名"); return; }
        if (!Regex.IsMatch(phone, @"^\d{6,20}$")) { Warn("请填写正确的客户电话（6-20 位数字）"); return; }
        if (DateBox.SelectedDate is not System.DateTime date) { Warn("请选择到店日期"); return; }
        // 时/分可下拉选择，也可直接输入；读文本并校验范围
        var hourText = (HourBox.Text ?? string.Empty).Trim();
        var minuteText = (MinuteBox.Text ?? string.Empty).Trim();
        if (!int.TryParse(hourText, out var hh) || hh < 0 || hh > 23
            || !int.TryParse(minuteText, out var mm) || mm < 0 || mm > 59)
        {
            Warn("请选择或输入正确的到店时间（小时 0-23，分钟 0-59）"); return;
        }
        var time = new TimeSpan(hh, mm, 0);
        var party = int.TryParse(PartyBox.Text, out var p) ? p : 0;
        if (party < 1 || party > 20) { Warn("人数需在 1 ~ 20 之间"); return; }

        var arriveAt = date.Date + time;
        var serviceId = ServiceBox.SelectedValue as long?;
        var techId = TechBox.SelectedValue as long?;
        var remark = string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim();

        try
        {
            SaveButton.IsEnabled = false;
            if (_isEdit && _prefill is not null)
            {
                await _api.UpdateAppointmentAsync(_prefill.Id, new UpdateAppointmentRequest(
                    ServiceId: serviceId, PreferredTechnicianId: techId,
                    CustomerName: name, CustomerPhone: phone,
                    ExpectedArriveAt: arriveAt, PartySize: party, Remark: remark));
            }
            else
            {
                await _api.CreateAppointmentAsync(new CreateAppointmentRequest(
                    StoreId: _storeId, ServiceId: serviceId, PreferredTechnicianId: techId,
                    CustomerName: name, CustomerPhone: phone, CustomerOpenId: null,
                    ExpectedArriveAt: arriveAt, PartySize: party, Remark: remark));
            }
            DialogResult = true;
            Close();
        }
        catch (System.Exception ex)
        {
            ErrorReporter.Show(ex);
            SaveButton.IsEnabled = true;
        }
    }

    private static void Warn(string msg) =>
        MessageBox.Show(msg, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    /// <summary>点击日期文本区域即展开日历（延迟置 true，避免被 DatePicker 自身输入处理收回）。</summary>
    private void DateBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
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
