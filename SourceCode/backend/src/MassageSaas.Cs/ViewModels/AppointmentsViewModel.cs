using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Appointments;

namespace MassageSaas.Cs.ViewModels;

public partial class AppointmentsViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public AppointmentsViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<AppointmentRowViewModel> rows = new();

    [ObservableProperty]
    private string? statusFilter;

    [ObservableProperty]
    private string? keyword;

    [ObservableProperty]
    private DateTime? fromDate;

    [ObservableProperty]
    private DateTime? toDate;

    [ObservableProperty]
    private bool isBusy;

    // ---- 分页（对齐 BS / 订单页） ----
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageInfo))]
    [NotifyPropertyChangedFor(nameof(CanPrev))]
    [NotifyPropertyChangedFor(nameof(CanNext))]
    private int page = 1;

    [ObservableProperty]
    private int pageSize = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageInfo))]
    [NotifyPropertyChangedFor(nameof(CanNext))]
    private int total;

    public int TotalPages => Total <= 0 ? 1 : (Total + PageSize - 1) / PageSize;
    public string PageInfo => $"第 {Page} / {TotalPages} 页 · 共 {Total} 条";
    public bool CanPrev => Page > 1;
    public bool CanNext => Page < TotalPages;

    /// <summary>重置时批量改筛选条件，用此标志压制每次属性变化都触发的自动查询，避免重复请求。</summary>
    private bool _suppressAutoReload;

    /// <summary>状态下拉框选值即查（回到第 1 页，对齐 BS：选择即筛选，无需再点查询）。</summary>
    partial void OnStatusFilterChanged(string? value)
    {
        if (_suppressAutoReload) return;
        Page = 1;
        _ = ReloadAsync();
    }

    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        try
        {
            var data = await _api.GetAppointmentsAsync(
                storeId: sid,
                status: string.IsNullOrEmpty(StatusFilter) ? null : StatusFilter,
                from: FromDate, to: ToDate,
                page: Page, pageSize: PageSize,
                keyword: string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim());
            Rows = new ObservableCollection<AppointmentRowViewModel>(
                data.Items.Select(a => new AppointmentRowViewModel(a)));
            Total = data.Total;
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    /// <summary>查询：回到第 1 页再查（关键字/日期条件变更时用，对齐 BS onSearch）。</summary>
    [RelayCommand]
    private async Task Search()
    {
        Page = 1;
        await ReloadAsync();
    }

    /// <summary>重置筛选条件并重新查询（对齐 BS）。</summary>
    [RelayCommand]
    private async Task ResetQuery()
    {
        _suppressAutoReload = true;
        StatusFilter = null;
        Keyword = null;
        FromDate = null;
        ToDate = null;
        Page = 1;
        _suppressAutoReload = false;
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task PrevPage()
    {
        if (!CanPrev) return;
        Page--;
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (!CanNext) return;
        Page++;
        await ReloadAsync();
    }

    /// <summary>登记电话预约（新建，状态待确认）。</summary>
    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (_context.ActiveStoreId is not long sid) { MessageBox.Show("请先选择门店"); return; }
        var dlg = new Views.AppointmentFormWindow(_api, sid, null, isEdit: false) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() == true) await ReloadAsync();
    }

    /// <summary>修改未确认（待确认）的预约。</summary>
    [RelayCommand]
    private async Task EditAsync(AppointmentRowViewModel? r)
    {
        if (r is null) return;
        if (!r.CanEdit) { MessageBox.Show("仅待确认的预约可修改", "提示"); return; }
        if (_context.ActiveStoreId is not long sid) return;
        var dlg = new Views.AppointmentFormWindow(_api, sid, r.Dto, isEdit: true) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() == true) await ReloadAsync();
    }

    /// <summary>基于已取消单再次预约：客人信息照搬、时间默认 30 分钟后。</summary>
    [RelayCommand]
    private async Task RebookAsync(AppointmentRowViewModel? r)
    {
        if (r is null) return;
        if (_context.ActiveStoreId is not long sid) { MessageBox.Show("请先选择门店"); return; }
        var dlg = new Views.AppointmentFormWindow(_api, sid, r.Dto, isEdit: false) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() == true) await ReloadAsync();
    }

    [RelayCommand]
    private async Task ConfirmAsync(AppointmentRowViewModel? r)
    {
        if (r is null) return;
        try
        {
            await _api.ConfirmAppointmentAsync(r.Dto.Id, new ConfirmAppointmentRequest(null));
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task ArriveAsync(AppointmentRowViewModel? r)
    {
        if (r is null) return;
        try
        {
            await _api.ArriveAppointmentAsync(r.Dto.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task CancelAsync(AppointmentRowViewModel? r)
    {
        if (r is null) return;
        if (MessageBox.Show($"确认取消 {r.CustomerName} 的预约？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
        try
        {
            await _api.CancelAppointmentAsync(r.Dto.Id, new CancelAppointmentRequest("门店端取消"));
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
