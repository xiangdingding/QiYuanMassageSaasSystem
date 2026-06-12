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

        // 年份/月份下拉选项：年份取近 6 年；月份取近 24 个月（YYYY-MM，新到旧），默认当前月
        var thisYear = DateTime.Today.Year;
        YearOptions = new ObservableCollection<int>(Enumerable.Range(0, 6).Select(i => thisYear - i));
        MonthOptions = new ObservableCollection<int>(Enumerable.Range(1, 12));

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

    /// <summary>生成工资单的目标月份（1-12）；年份取上方「年份」下拉。默认当前月。</summary>
    [ObservableProperty]
    private int genMonthNumber = DateTime.Today.Month;

    /// <summary>年份下拉项（近 6 年）：既是列表筛选年份，也是生成工资单的目标年份。</summary>
    [ObservableProperty]
    private ObservableCollection<int> yearOptions = new();

    /// <summary>月份下拉项（1-12）。</summary>
    [ObservableProperty]
    private ObservableCollection<int> monthOptions = new();

    [ObservableProperty]
    private bool isBusy;

    /// <summary>工资单页标题：显示当前门店名（与 BS「{门店} 工资单」一致）。</summary>
    public string PeriodsTitle => string.IsNullOrWhiteSpace(_context.ActiveStore?.Name)
        ? "工资单" : $"{_context.ActiveStore!.Name} 工资单";

    /// <summary>薪资配置页标题：显示当前门店名（与 BS「薪资配置（{门店}）」一致）。</summary>
    public string ProfilesTitle => string.IsNullOrWhiteSpace(_context.ActiveStore?.Name)
        ? "薪资配置" : $"薪资配置（{_context.ActiveStore!.Name}）";

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
                if (still is not null) await LoadDetailAsync(still);
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

    /// <summary>「查看」按钮：加载明细后弹出独立大窗（DataContext 复用本 VM，窗内命令直接作用于同一份明细）。</summary>
    [RelayCommand]
    private async Task ViewDetailAsync(PayrollPeriodDto? period)
    {
        if (period is null) return;
        await LoadDetailAsync(period);
        if (CurrentPeriod is null) return;
        var win = new Views.PayrollDetailWindow
        {
            DataContext = this,
            Owner = Application.Current?.MainWindow
        };
        win.ShowDialog();
    }

    /// <summary>仅加载某月工资单明细到 CurrentPeriod/DetailItems，不弹窗——供刷新/重载复用。</summary>
    private async Task LoadDetailAsync(PayrollPeriodDto? period)
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

    /// <summary>生成工资单：与 BS 一致——直接按工具栏内嵌月份生成草稿（无弹窗、无备注），
    /// 未来月份/重复由后端校验并提示。生成后刷新列表并提示，不自动弹明细。</summary>
    [RelayCommand]
    private async Task GenerateAsync()
    {
        if (_context.ActiveStoreId is not long sid) { MessageBox.Show("未选择门店"); return; }
        try
        {
            await _api.GeneratePayrollAsync(new GeneratePayrollRequest(sid, YearFilter, GenMonthNumber, null));
            await ReloadAsync();
            MessageBox.Show("已生成草稿，请进入查看明细", "已生成", MessageBoxButton.OK, MessageBoxImage.Information);
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
        await LoadDetailAsync(CurrentPeriod);
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
            MessageBox.Show("已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
