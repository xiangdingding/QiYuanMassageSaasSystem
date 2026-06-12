using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Commissions;
using MassageSaas.Shared.Services;
using MassageSaas.Shared.Staff;
using MassageSaas.Shared.Settings;

namespace MassageSaas.Cs.ViewModels;

public partial class CommissionsViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    /// <summary>过滤下拉项：Id 为 null 表示"全部"。</summary>
    public record FilterOption(long? Id, string Label);

    public CommissionsViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = LoadFiltersAsync();
        _ = ReloadAsync();
        _ = LoadReferralAsync();
    }

    [ObservableProperty]
    private ObservableCollection<CommissionRuleRowViewModel> rows = new();

    [ObservableProperty]
    private bool isBusy;

    /// <summary>表头全选：联动设置所有行的勾选状态。</summary>
    [ObservableProperty]
    private bool allChecked;

    partial void OnAllCheckedChanged(bool value)
    {
        foreach (var r in Rows) r.IsChecked = value;
    }

    // ---- 列表过滤（对齐 BS：按服务 / 按技师过滤 + 查询）----
    [ObservableProperty]
    private ObservableCollection<FilterOption> serviceOptions = new();
    [ObservableProperty]
    private ObservableCollection<FilterOption> technicianOptions = new();
    /// <summary>当前选中的过滤服务 Id；null = 全部服务。</summary>
    [ObservableProperty]
    private long? filterServiceId;
    /// <summary>当前选中的过滤技师 Id；null = 全部技师。</summary>
    [ObservableProperty]
    private long? filterTechnicianId;

    private async Task LoadFiltersAsync()
    {
        try
        {
            var services = await _api.GetServicesAsync(false);
            var techs = await _api.GetStaffAsync(role: "Technician", pageSize: 200, storeId: _context.ActiveStoreId);

            var svcOpts = new ObservableCollection<FilterOption> { new(null, "全部服务") };
            foreach (var s in services) svcOpts.Add(new FilterOption(s.Id, $"{s.Code} {s.Name}"));
            ServiceOptions = svcOpts;

            var techOpts = new ObservableCollection<FilterOption> { new(null, "全部技师") };
            foreach (var t in techs.Items) techOpts.Add(new FilterOption(t.Id, $"{(t.EmployeeNo?.ToString() ?? "-")} · {t.RealName ?? t.Username}"));
            TechnicianOptions = techOpts;
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    // ---- 推荐规则（全店统一，规则模块第二 Tab）----

    /// <summary>顾客推荐返佣模式：None / PercentPerRecharge / FixedPerCard（百分比与固定二选一）。</summary>
    [ObservableProperty] private string refCustomerMode = "None";
    /// <summary>顾客推荐：每次充值返佣百分比（0-100）。仅模式=PercentPerRecharge 时生效。</summary>
    [ObservableProperty] private double refCustomerPercent;
    /// <summary>顾客推荐：开卡一次性固定推荐费/张。仅模式=FixedPerCard 时生效。</summary>
    [ObservableProperty] private double refCustomerFixed;
    /// <summary>员工推荐提成模式：None / FixedPerCard / PercentOfOpenCard。</summary>
    [ObservableProperty] private string refStaffMode = "None";
    /// <summary>员工推荐提成·固定金额/张。仅模式=FixedPerCard 时生效。</summary>
    [ObservableProperty] private double refStaffFixed;
    /// <summary>员工推荐提成·开卡实收百分比（0-100）。仅模式=PercentOfOpenCard 时生效。</summary>
    [ObservableProperty] private double refStaffPercent;

    private async Task LoadReferralAsync()
    {
        try
        {
            var s = await _api.GetReferralSettingsAsync();
            RefCustomerMode = s.CustomerReferralMode;
            RefCustomerPercent = (double)s.CustomerRewardPercent;
            RefCustomerFixed = (double)s.CustomerFixedReward;
            RefStaffMode = s.StaffReferralMode;
            RefStaffFixed = (double)s.StaffReferralFixedAmount;
            RefStaffPercent = (double)s.StaffReferralPercent;
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task SaveReferralAsync()
    {
        try
        {
            var saved = await _api.UpdateReferralSettingsAsync(new UpdateReferralSettingRequest(
                RefCustomerMode, (decimal)RefCustomerPercent, (decimal)RefCustomerFixed,
                RefStaffMode, (decimal)RefStaffFixed, (decimal)RefStaffPercent));
            RefCustomerMode = saved.CustomerReferralMode;
            RefCustomerPercent = (double)saved.CustomerRewardPercent;
            RefCustomerFixed = (double)saved.CustomerFixedReward;
            RefStaffMode = saved.StaffReferralMode;
            RefStaffFixed = (double)saved.StaffReferralFixedAmount;
            RefStaffPercent = (double)saved.StaffReferralPercent;
            MessageBox.Show("推荐规则已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            var list = await _api.GetCommissionRulesAsync(FilterServiceId, FilterTechnicianId);
            Rows = new ObservableCollection<CommissionRuleRowViewModel>(
                list.Select(r => new CommissionRuleRowViewModel(r)));
            AllChecked = false;
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var dlg = new Views.CommissionFormWindow(_api, _context.ActiveStoreId, null) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateCommissionRuleAsync(dlg.BuildCreateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>批量设置：服务 × 技师笛卡尔积按同一模板生成/更新通用规则。</summary>
    [RelayCommand]
    private async Task BulkSetAsync()
    {
        var dlg = new Views.BulkCommissionWindow(_api, _context.ActiveStoreId) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() == true) await ReloadAsync();
    }

    [RelayCommand]
    private async Task EditAsync(CommissionRuleRowViewModel? row)
    {
        if (row is null) return;
        var dlg = new Views.CommissionFormWindow(_api, _context.ActiveStoreId, row.Dto) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.UpdateCommissionRuleAsync(row.Dto.Id, dlg.BuildUpdateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task RemoveAsync(CommissionRuleRowViewModel? row)
    {
        if (row is null) return;
        if (row.Dto.IsActive) { MessageBox.Show("请先禁用该提成规则，再删除。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (MessageBox.Show("确认删除提成规则？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning)
            != MessageBoxResult.OK) return;
        try
        {
            await _api.DeleteCommissionRuleAsync(row.Dto.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>批量启用选中的提成规则。</summary>
    [RelayCommand]
    private Task BatchEnableAsync() => BatchSetActiveAsync(true);

    /// <summary>批量禁用选中的提成规则。</summary>
    [RelayCommand]
    private Task BatchDisableAsync() => BatchSetActiveAsync(false);

    private async Task BatchSetActiveAsync(bool isActive)
    {
        var ids = Rows.Where(r => r.IsChecked).Select(r => r.Dto.Id).ToArray();
        if (ids.Length == 0) { MessageBox.Show("请先勾选要操作的提成规则", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        try
        {
            var result = await _api.BulkUpdateCommissionStatusAsync(new BulkCommissionStatusRequest(ids, isActive));
            await ReloadAsync();
            MessageBox.Show($"已{(isActive ? "启用" : "禁用")} {result.Updated} 条规则", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>批量删除勾选的提成规则（启用中的会被跳过，需先禁用）。</summary>
    [RelayCommand]
    private async Task BatchDeleteAsync()
    {
        var ids = Rows.Where(r => r.IsChecked).Select(r => r.Dto.Id).ToArray();
        if (ids.Length == 0) { MessageBox.Show("请先勾选要删除的提成规则", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (MessageBox.Show($"确认删除选中的 {ids.Length} 条提成规则？启用中的规则将被跳过。", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning)
            != MessageBoxResult.OK) return;
        try
        {
            var result = await _api.BulkDeleteCommissionRulesAsync(new BulkCommissionDeleteRequest(ids));
            await ReloadAsync();
            var msg = $"已删除 {result.Deleted} 条规则";
            if (result.SkippedActive > 0) msg += $"，{result.SkippedActive} 条因启用中被跳过（请先禁用）";
            MessageBox.Show(msg, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
