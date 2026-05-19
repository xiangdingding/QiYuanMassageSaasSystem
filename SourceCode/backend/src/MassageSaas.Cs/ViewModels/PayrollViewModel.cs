using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Payroll;

namespace MassageSaas.Cs.ViewModels;

/// <summary>工资结算：按月生成工资单（底薪+提成+加班+满勤+奖扣），封盘、发放；以及薪资档案配置。</summary>
public partial class PayrollViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public PayrollViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<PayrollPeriodDto> periods = new();

    [ObservableProperty]
    private ObservableCollection<PayrollItemDto> detailItems = new();

    [ObservableProperty]
    private ObservableCollection<SalaryProfileDto> profiles = new();

    [ObservableProperty]
    private PayrollPeriodDto? currentPeriod;

    [ObservableProperty]
    private PayrollItemDto? selectedItem;

    [ObservableProperty]
    private int yearFilter = DateTime.Today.Year;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>当前查看的工资单是否为草稿——只有草稿可改条目 / 录奖扣。</summary>
    public bool CanEditCurrent => CurrentPeriod?.Status == "Draft";

    partial void OnYearFilterChanged(int value) => _ = ReloadAsync();

    partial void OnCurrentPeriodChanged(PayrollPeriodDto? value) => OnPropertyChanged(nameof(CanEditCurrent));

    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        try
        {
            Periods = new ObservableCollection<PayrollPeriodDto>(
                await _api.GetPayrollPeriodsAsync(sid, YearFilter));
            await LoadProfilesAsync(sid);
            if (CurrentPeriod is not null)
            {
                var still = Periods.FirstOrDefault(p => p.Id == CurrentPeriod.Id);
                if (still is not null) await ViewDetailAsync(still);
                else ClearDetail();
            }
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    private async Task LoadProfilesAsync(long storeId)
    {
        // 员工可能从未配过薪资，与员工表合并后用 0 占位，便于一处补齐
        var staff = await _api.GetStaffAsync(pageSize: 200, storeId: storeId);
        var existing = (await _api.GetSalaryProfilesAsync(storeId)).ToDictionary(p => p.UserId);
        Profiles = new ObservableCollection<SalaryProfileDto>(
            staff.Items
                .Where(s => s.Role is "Technician" or "Cashier" or "StoreManager")
                .Select(s => existing.TryGetValue(s.Id, out var p)
                    ? p
                    : new SalaryProfileDto(s.Id, s.RealName ?? s.Username, 0m, 0m, 0m, 0, null)));
    }

    private void ClearDetail()
    {
        CurrentPeriod = null;
        DetailItems = new();
        SelectedItem = null;
    }

    [RelayCommand]
    private async Task ViewDetailAsync(PayrollPeriodDto? period)
    {
        if (period is null) return;
        try
        {
            var detail = await _api.GetPayrollPeriodAsync(period.Id);
            CurrentPeriod = detail.Period;
            DetailItems = new ObservableCollection<PayrollItemDto>(detail.Items);
            SelectedItem = null;
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        if (_context.ActiveStoreId is not long sid) { MessageBox.Show("未选择门店"); return; }
        var dlg = new Views.PayrollGenerateWindow();
        if (dlg.ShowDialog() != true) return;
        try
        {
            var detail = await _api.GeneratePayrollAsync(
                new GeneratePayrollRequest(sid, dlg.Year, dlg.Month, dlg.Remark));
            await ReloadAsync();
            await ViewDetailAsync(detail.Period);
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task LockAsync(PayrollPeriodDto? period)
    {
        if (period is null) return;
        if (period.Status != "Draft") { MessageBox.Show("仅草稿可封盘"); return; }
        if (MessageBox.Show($"封盘后 {period.Year}-{period.Month:D2} 工资单不可再改。确认？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.LockPayrollAsync(period.Id, new LockPayrollRequest(null));
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task MarkPaidAsync(PayrollPeriodDto? period)
    {
        if (period is null) return;
        if (period.Status != "Locked") { MessageBox.Show("仅封盘后的工资单可标记发放"); return; }
        if (MessageBox.Show($"确认 {period.Year}-{period.Month:D2} 工资已发放？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
        try
        {
            await _api.MarkPayrollPaidAsync(period.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task DeleteDraftAsync(PayrollPeriodDto? period)
    {
        if (period is null) return;
        if (period.Status != "Draft") { MessageBox.Show("仅草稿可删除"); return; }
        if (MessageBox.Show($"删除 {period.Year}-{period.Month:D2} 工资单草稿？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.DeletePayrollDraftAsync(period.Id);
            if (CurrentPeriod?.Id == period.Id) ClearDetail();
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task EditItemAsync(PayrollItemDto? item)
    {
        if (item is null) return;
        if (!CanEditCurrent) { MessageBox.Show("仅草稿状态可修改工资项"); return; }
        var dlg = new Views.PayrollItemEditWindow(item);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.UpdatePayrollItemAsync(item.Id, dlg.BuildRequest());
            await RefreshDetailAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task AddAdjustmentAsync(PayrollItemDto? item)
    {
        if (item is null) return;
        if (!CanEditCurrent) { MessageBox.Show("仅草稿状态可录入奖扣"); return; }
        var dlg = new Views.PayrollAdjustmentWindow(item.UserName);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.AddPayrollAdjustmentAsync(item.Id, dlg.BuildRequest());
            await RefreshDetailAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task RemoveAdjustmentAsync(PayrollAdjustmentDto? adj)
    {
        if (adj is null || SelectedItem is null) return;
        if (!CanEditCurrent) { MessageBox.Show("仅草稿状态可删除奖扣"); return; }
        if (MessageBox.Show($"删除「{adj.Reason}」这笔{(adj.Kind == "Bonus" ? "奖金" : "扣款")}？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.RemovePayrollAdjustmentAsync(SelectedItem.Id, adj.Id);
            await RefreshDetailAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    private async Task RefreshDetailAsync()
    {
        if (CurrentPeriod is null) return;
        var keepItemId = SelectedItem?.Id;
        await ViewDetailAsync(CurrentPeriod);
        if (keepItemId is long id)
            SelectedItem = DetailItems.FirstOrDefault(i => i.Id == id);
    }

    [RelayCommand]
    private async Task EditProfileAsync(SalaryProfileDto? profile)
    {
        if (profile is null) return;
        var dlg = new Views.SalaryProfileWindow(profile);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.UpsertSalaryProfileAsync(profile.UserId, dlg.BuildRequest());
            if (_context.ActiveStoreId is long sid) await LoadProfilesAsync(sid);
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
