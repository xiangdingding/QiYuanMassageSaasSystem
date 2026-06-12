using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Staff;
using MassageSaas.Shared.Stores;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 新建 / 编辑员工。字段与逻辑对齐 BS StaffView 的表单弹窗：
/// 手机号用作登录账号；身份证号必填且自动带出出生日期；含紧急联系人、入职/离职日期、所属门店、专长多选。
/// </summary>
public partial class StaffFormWindow : Window
{
    private readonly IApiClient _api;
    private readonly StaffDto? _editing;
    private readonly long _defaultStoreId;
    private readonly List<ToggleButton> _specialtyBoxes = new();

    /// <summary>门店下拉项。</summary>
    private record StoreOpt(long? Id, string Label);

    /// <summary>可选专长清单（与 BS SPECIALTY_OPTIONS 一致）。</summary>
    private static readonly string[] SpecialtyOptions =
    {
        "肩颈", "足疗", "头疗", "推拿", "按摩", "艾灸", "拔罐", "刮痧",
        "中医理疗", "泰式按摩", "精油 SPA", "产后修复", "小儿推拿", "盲人按摩"
    };

    public StaffFormWindow(IApiClient api, StaffDto? editing, long storeId, IReadOnlyList<StoreDto> stores, int nextEmployeeNo = 0)
    {
        InitializeComponent();
        _api = api;
        _editing = editing;
        _defaultStoreId = storeId;

        StoreBox.ItemsSource = stores.Select(s => new StoreOpt(s.Id, s.Name)).ToList();
        BuildSpecialtyBoxes();

        if (editing is not null)
        {
            Title = $"编辑员工 - {editing.Username}";
            PhoneBox.Text = editing.Phone ?? string.Empty;
            RealNameBox.Text = editing.RealName ?? string.Empty;
            EmployeeNoBox.Value = editing.EmployeeNo ?? 0;
            IdCardBox.Text = editing.IdCardNo ?? string.Empty;
            BirthDatePicker.SelectedDate = editing.BirthDate;
            EmergencyNameBox.Text = editing.EmergencyContactName ?? string.Empty;
            EmergencyPhoneBox.Text = editing.EmergencyContactPhone ?? string.Empty;
            HireDatePicker.SelectedDate = editing.HireDate;
            TerminationDatePicker.SelectedDate = editing.TerminationDate;
            RoleBox.SelectedValue = editing.Role;
            StoreBox.SelectedValue = editing.StoreId ?? storeId;
            BlindToggle.IsChecked = editing.IsBlind;
            ActiveToggle.IsChecked = editing.IsActive;
            CheckSpecialties(editing.Specialties);

            // 编辑不改密码
            PasswordCell.Visibility = Visibility.Collapsed;
        }
        else
        {
            Title = "新建员工";
            RoleBox.SelectedValue = "Technician";
            StoreBox.SelectedValue = storeId;
            ActiveToggle.IsChecked = true;
            // 工号默认 = 现有最大工号 + 1（由调用方算好传入）
            EmployeeNoBox.Value = nextEmployeeNo;
        }
    }

    private void BuildSpecialtyBoxes()
    {
        var chipStyle = (Style)FindResource("ChipToggle");
        foreach (var opt in SpecialtyOptions)
        {
            var chip = new ToggleButton { Content = opt, Style = chipStyle };
            chip.SetValue(AutomationProperties.NameProperty, $"专长 {opt}");
            _specialtyBoxes.Add(chip);
            SpecialtyPanel.Children.Add(chip);
        }
    }

    private void CheckSpecialties(string? specialties)
    {
        if (string.IsNullOrWhiteSpace(specialties)) return;
        var set = specialties.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).ToHashSet();
        foreach (var cb in _specialtyBoxes)
            if (cb.Content is string s && set.Contains(s)) cb.IsChecked = true;
    }

    /// <summary>身份证 7~14 位 = YYYYMMDD，自动带出出生日期（与 BS onIdCardChange 一致）。</summary>
    private void IdCard_TextChanged(object sender, TextChangedEventArgs e)
    {
        var v = IdCardBox.Text?.Trim() ?? string.Empty;
        if (v.Length < 14) return;
        var ymd = v.Substring(6, 8);
        if (DateTime.TryParseExact(ymd, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
            BirthDatePicker.SelectedDate = dob;
    }

    private string Role => (RoleBox.SelectedValue as string) ?? "Technician";

    private long StoreId =>
        StoreBox.SelectedValue is long id ? id : _defaultStoreId;

    private int? EmployeeNo => (int)EmployeeNoBox.Value is var n && n > 0 ? n : null;

    private string? Specialties()
    {
        var picked = _specialtyBoxes
            .Where(c => c.IsChecked == true && c.Content is string)
            .Select(c => (string)c.Content);
        var joined = string.Join(",", picked);
        return string.IsNullOrWhiteSpace(joined) ? null : joined;
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public CreateStaffRequest BuildCreateRequest() => new(
        StoreId: StoreId,
        Username: PhoneBox.Text.Trim(),     // 手机号即登录账号
        Password: PasswordBox.Password,
        Role: Role,
        RealName: NullIfBlank(RealNameBox.Text),
        Phone: NullIfBlank(PhoneBox.Text),
        EmployeeNo: EmployeeNo,
        IsBlind: BlindToggle.IsChecked == true,
        Specialties: Specialties(),
        IdCardNo: NullIfBlank(IdCardBox.Text),
        BirthDate: BirthDatePicker.SelectedDate,
        EmergencyContactName: NullIfBlank(EmergencyNameBox.Text),
        EmergencyContactPhone: NullIfBlank(EmergencyPhoneBox.Text),
        HireDate: HireDatePicker.SelectedDate,
        TerminationDate: TerminationDatePicker.SelectedDate);

    public UpdateStaffRequest BuildUpdateRequest() => new(
        StoreId: StoreId,
        Role: Role,
        RealName: NullIfBlank(RealNameBox.Text),
        Phone: NullIfBlank(PhoneBox.Text),
        EmployeeNo: EmployeeNo,
        IsBlind: BlindToggle.IsChecked == true,
        IsActive: ActiveToggle.IsChecked == true,
        Specialties: Specialties(),
        IdCardNo: NullIfBlank(IdCardBox.Text),
        BirthDate: BirthDatePicker.SelectedDate,
        EmergencyContactName: NullIfBlank(EmergencyNameBox.Text),
        EmergencyContactPhone: NullIfBlank(EmergencyPhoneBox.Text),
        HireDate: HireDatePicker.SelectedDate,
        TerminationDate: TerminationDatePicker.SelectedDate);

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var phone = PhoneBox.Text.Trim();
        if (!Regex.IsMatch(phone, @"^\d{11}$"))
        {
            MessageBox.Show("请输入 11 位手机号（用作登录账号）");
            return;
        }
        if (string.IsNullOrWhiteSpace(RealNameBox.Text))
        {
            MessageBox.Show("请输入姓名");
            return;
        }
        if (!Regex.IsMatch(IdCardBox.Text.Trim(), @"^\d{17}[\dXx]$"))
        {
            MessageBox.Show("请输入正确的 18 位身份证号");
            return;
        }
        if (StoreBox.SelectedValue is null)
        {
            MessageBox.Show("请选择所属门店");
            return;
        }
        if (_editing is null && PasswordBox.Password.Length < 6)
        {
            MessageBox.Show("新建员工需设定至少 6 位初始密码");
            return;
        }

        // 窗口自己调接口：成功才关闭；「用户已存在」等异常只提示、保持窗口打开，输入不丢失
        var btn = sender as Button;
        if (btn is not null) btn.IsEnabled = false;
        try
        {
            if (_editing is null)
                await _api.CreateStaffAsync(BuildCreateRequest());
            else
                await _api.UpdateStaffAsync(_editing.Id, BuildUpdateRequest());
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorReporter.Show(ex);
        }
        finally
        {
            if (btn is not null) btn.IsEnabled = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
