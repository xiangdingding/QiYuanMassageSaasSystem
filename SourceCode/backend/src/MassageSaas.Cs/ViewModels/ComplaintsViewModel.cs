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

    partial void OnStatusFilterChanged(int value) => _ = ReloadAsync();

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
            var paged = await _api.GetComplaintsAsync(storeId: sid, status: status, pageSize: 100);
            Rows = new ObservableCollection<ComplaintDto>(paged.Items);

            var staff = await _api.GetStaffAsync(pageSize: 100, role: "Technician", storeId: sid);
            _technicians = staff.Items.Where(s => s.IsActive).ToList();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
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
