using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Complaints;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.ViewModels;

/// <summary>投诉处理：登记的客诉单按状态查看，支持改派 / 退款 / 道歉 / 不予处理与撤销。</summary>
public partial class ComplaintsViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;
    private List<StaffDto> _technicians = new();

    public ComplaintsViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<ComplaintDto> rows = new();

    /// <summary>状态筛选：0 全部，1 待处理，2 已处理，3 已撤销。</summary>
    [ObservableProperty]
    private int statusFilter = 1;

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

    /// <summary>状态下拉选值即查，回到第 1 页。</summary>
    partial void OnStatusFilterChanged(int value)
    {
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
            var status = StatusFilter switch
            {
                1 => "Pending",
                2 => "Resolved",
                3 => "Cancelled",
                _ => null
            };
            var paged = await _api.GetComplaintsAsync(storeId: sid, status: status, page: Page, pageSize: PageSize);
            Rows = new ObservableCollection<ComplaintDto>(paged.Items);
            Total = paged.Total;

            var staff = await _api.GetStaffAsync(pageSize: 100, role: "Technician", storeId: sid);
            _technicians = staff.Items.Where(s => s.IsActive).ToList();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
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

    /// <summary>登记投诉：查询订单(按技师+日期)选服务项登记，或不指定项目做匿名投诉。</summary>
    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (_context.ActiveStoreId is not long sid) { MessageBox.Show("请先选择门店"); return; }
        var dlg = new Views.ComplaintCreateWindow(_api, sid, _technicians) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() == true) await ReloadAsync();
    }

    [RelayCommand]
    private async Task ResolveAsync(ComplaintDto? c)
    {
        if (c is null) return;
        if (c.Status != "Pending") { MessageBox.Show("仅待处理的投诉可处理"); return; }
        var dlg = new Views.ComplaintResolveWindow(c, _technicians);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.ResolveComplaintAsync(c.Id, dlg.BuildRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task CancelAsync(ComplaintDto? c)
    {
        if (c is null) return;
        if (c.Status != "Pending") { MessageBox.Show("仅待处理的投诉可撤销"); return; }
        if (MessageBox.Show("确认撤销该投诉单？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.CancelComplaintAsync(c.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
