using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Members;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 编辑会员资料（对齐 BS 端 openEdit / saveForm 的编辑分支）：
/// 可改 手机号 / 姓名 / 性别 / 生日 / 引荐人 / 备注；卡号只读；折扣与其它隐藏字段沿用原值不动。
/// </summary>
public partial class MemberFormWindow : Window
{
    private readonly IApiClient _api;
    private readonly MemberDto _editing;
    private long? _referrerMemberId;
    private long? _referrerStaffId;

    /// <summary>员工推荐人下拉项。Display = "编号 姓名"（无编号时只姓名）。</summary>
    private sealed record StaffOption(long? Id, string Display);

    public MemberFormWindow(IApiClient api, MemberDto editing)
    {
        InitializeComponent();
        _api = api;
        _editing = editing;
        Title = $"编辑会员 - {editing.CardNo}";
        PhoneBox.Text = editing.Phone;
        CardNoBox.Text = editing.CardNo;
        NameBox.Text = editing.Name ?? string.Empty;
        BirthdayBox.SelectedDate = editing.Birthday;
        RemarkBox.Text = editing.Remark ?? string.Empty;
        SelectGender(editing.Gender);
        // 顾客引荐人：沿用原值并直接在下拉控件里带出当前引荐人（可重新搜索改人、清空即清除）
        _referrerMemberId = editing.ReferredByMemberId;
        ReferrerSelLabel.Text = string.IsNullOrWhiteSpace(editing.ReferredByMemberName) ? "无" : editing.ReferredByMemberName!;
        if (_referrerMemberId.HasValue)
        {
            var label = string.IsNullOrWhiteSpace(editing.ReferredByMemberName)
                ? $"会员 #{_referrerMemberId}" : editing.ReferredByMemberName!;
            var seed = new ReferrerOption(label, _referrerMemberId);
            ReferrerBox.ItemsSource = new List<ReferrerOption> { seed };
            ReferrerBox.SelectedItem = seed;
        }
        // 员工推荐人：沿用原值，加载本店在职员工后预选当前推荐人
        _referrerStaffId = editing.ReferredByStaffId;
        _ = LoadStaffAsync();
    }

    /// <summary>加载本店在职员工填充员工推荐下拉，并预选会员当前的员工推荐人。</summary>
    private async System.Threading.Tasks.Task LoadStaffAsync()
    {
        try
        {
            var staff = await _api.GetStaffAsync(pageSize: 200, storeId: _editing.StoreId);
            var opts = new List<StaffOption> { new(null, "不指定") };
            opts.AddRange(staff.Items.Where(s => s.IsActive)
                .Select(s => new StaffOption(s.Id,
                    s.EmployeeNo is int no ? $"{no} {s.RealName ?? s.Username}" : (s.RealName ?? s.Username))));
            StaffReferrerBox.ItemsSource = opts;
            var current = opts.FirstOrDefault(o => o.Id == _referrerStaffId) ?? opts[0];
            StaffReferrerBox.SelectedItem = current;
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    private void StaffReferrer_Picked(object sender, SelectionChangedEventArgs e) =>
        _referrerStaffId = (StaffReferrerBox.SelectedItem as StaffOption)?.Id;

    private void SelectGender(string? gender)
    {
        foreach (var c in GenderPanel.Children)
            if (c is RadioButton rb)
                rb.IsChecked = (rb.CommandParameter as string) == gender;
    }

    private string? Gender()
    {
        foreach (var c in GenderPanel.Children)
            if (c is RadioButton { IsChecked: true } rb && rb.CommandParameter is string g) return g;
        return null;
    }

    // ---- 引荐人：可输入下拉，输入即时远程搜索、按人聚合、选中即用（与开卡一致）----

    private sealed record ReferrerOption(string Label, long? MemberId);

    private System.Windows.Threading.DispatcherTimer? _refTimer;

    private void ReferrerBox_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Up or Key.Down or Key.Enter or Key.Tab or Key.Escape or Key.LeftShift or Key.RightShift) return;
        var text = ReferrerBox.Text?.Trim() ?? "";
        if (text.Length == 0)
        {
            _referrerMemberId = null;
            ReferrerSelLabel.Text = "无";
            ReferrerBox.ItemsSource = null;
            ReferrerBox.IsDropDownOpen = false;
            return;
        }
        _refTimer ??= new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _refTimer.Tick -= RefTimer_Tick;
        _refTimer.Tick += RefTimer_Tick;
        _refTimer.Stop();
        _refTimer.Start();
    }

    private void RefTimer_Tick(object? sender, EventArgs e)
    {
        _refTimer?.Stop();
        _ = SearchReferrerAsync(ReferrerBox.Text?.Trim() ?? "");
    }

    private async System.Threading.Tasks.Task SearchReferrerAsync(string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) { ReferrerBox.ItemsSource = null; return; }
        try
        {
            var r = await _api.GetMemberGroupedAsync(page: 1, pageSize: 10, keyword: keyword, storeId: _editing.StoreId, includeClosed: false);
            var opts = r.Items.Select(g => new ReferrerOption(
                $"{g.Phone} · {g.PrimaryName ?? "未填"} · {g.CardCount} 张卡",
                (g.Cards.FirstOrDefault(c => c.IsActive) ?? g.Cards.FirstOrDefault())?.Id)).ToList();
            ReferrerBox.ItemsSource = opts;
            ReferrerBox.IsDropDownOpen = opts.Count > 0;
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    private void Referrer_Picked(object sender, SelectionChangedEventArgs e)
    {
        if (ReferrerBox.SelectedItem is not ReferrerOption opt) return;
        _referrerMemberId = opt.MemberId;
        ReferrerSelLabel.Text = opt.Label;
    }

    /// <summary>折扣与 Level/偏好/健康/微信 等不在本窗编辑的字段一律沿用原值，避免被清空。</summary>
    public UpdateMemberRequest BuildUpdateRequest() => new(
        Phone: PhoneBox.Text.Trim(),
        Name: string.IsNullOrWhiteSpace(NameBox.Text) ? null : NameBox.Text.Trim(),
        Gender: Gender(),
        Birthday: BirthdayBox.SelectedDate,
        Discount: _editing.Discount,
        Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim(),
        Level: _editing.Level,
        PreferenceNotes: _editing.PreferenceNotes,
        HealthNotes: _editing.HealthNotes,
        ReferredByMemberId: _referrerMemberId,
        WechatOpenId: _editing.WechatOpenId,
        ReferredByStaffId: _referrerStaffId,
        UpdateStaffReferral: true,
        UpdateReferredByMember: true);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PhoneBox.Text))
        {
            MessageBox.Show("手机号必填", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
