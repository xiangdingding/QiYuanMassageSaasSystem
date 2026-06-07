using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Commissions;
using MassageSaas.Shared.Settings;

namespace MassageSaas.Cs.ViewModels;

public partial class CommissionsViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public CommissionsViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
        _ = LoadReferralAsync();
    }

    [ObservableProperty]
    private ObservableCollection<CommissionRuleDto> rows = new();

    [ObservableProperty]
    private bool isBusy;

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
            Rows = new ObservableCollection<CommissionRuleDto>(await _api.GetCommissionRulesAsync());
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var dlg = new Views.CommissionFormWindow(null);
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
    private async Task EditAsync(CommissionRuleDto? r)
    {
        if (r is null) return;
        var dlg = new Views.CommissionFormWindow(r);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.UpdateCommissionRuleAsync(r.Id, dlg.BuildUpdateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task RemoveAsync(CommissionRuleDto? r)
    {
        if (r is null) return;
        if (MessageBox.Show("确认删除提成规则？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning)
            != MessageBoxResult.OK) return;
        try
        {
            await _api.DeleteCommissionRuleAsync(r.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
