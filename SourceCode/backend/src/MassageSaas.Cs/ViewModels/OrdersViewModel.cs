using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Orders;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.ViewModels;

public partial class OrdersViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public OrdersViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<OrderListItemDto> rows = new();

    [ObservableProperty]
    private string? statusFilter;

    [ObservableProperty]
    private DateTime? fromDate;

    [ObservableProperty]
    private DateTime? toDate;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private OrderDto? selectedDetail;

    [ObservableProperty]
    private ObservableCollection<StaffDto> technicians = new();

    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        try
        {
            var data = await _api.GetOrdersAsync(
                page: 1, pageSize: 100,
                storeId: sid,
                status: string.IsNullOrEmpty(StatusFilter) ? null : StatusFilter,
                from: FromDate,
                to: ToDate);
            Rows = new ObservableCollection<OrderListItemDto>(data.Items);
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ShowDetailAsync(OrderListItemDto? row)
    {
        if (row is null) return;
        try
        {
            SelectedDetail = await _api.GetOrderAsync(row.Id);
            if (Technicians.Count == 0)
            {
                var t = await _api.GetStaffAsync(role: "Technician", pageSize: 200, storeId: _context.ActiveStoreId);
                Technicians = new ObservableCollection<StaffDto>(t.Items);
            }
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task RefundAsync()
    {
        if (SelectedDetail is null) return;
        if (MessageBox.Show("确认退款？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Question)
            != MessageBoxResult.OK) return;
        try
        {
            await _api.RefundAsync(SelectedDetail.Id, new RefundRequest("门店退款"));
            MessageBox.Show("已退款");
            SelectedDetail = null;
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (SelectedDetail is null) return;
        if (MessageBox.Show("确认取消订单？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Question)
            != MessageBoxResult.OK) return;
        try
        {
            await _api.CancelOrderAsync(SelectedDetail.Id);
            MessageBox.Show("已取消");
            SelectedDetail = null;
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task TransferAsync(object[]? args)
    {
        if (args is null || args.Length < 2) return;
        if (args[0] is not OrderItemDto item) return;
        if (args[1] is not StaffDto newTech) return;
        if (SelectedDetail is null) return;
        try
        {
            SelectedDetail = await _api.TransferTechnicianAsync(SelectedDetail.Id, item.Id,
                new TransferTechnicianRequest(newTech.Id, "门店端转钟"));
            MessageBox.Show("转钟成功");
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
