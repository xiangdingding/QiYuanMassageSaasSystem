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
    private ObservableCollection<AppointmentDto> rows = new();

    [ObservableProperty]
    private string? statusFilter;

    [ObservableProperty]
    private DateTime? fromDate;

    [ObservableProperty]
    private DateTime? toDate;

    [ObservableProperty]
    private bool isBusy;

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
                page: 1, pageSize: 100);
            Rows = new ObservableCollection<AppointmentDto>(data.Items);
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ConfirmAsync(AppointmentDto? a)
    {
        if (a is null) return;
        try
        {
            await _api.ConfirmAppointmentAsync(a.Id, new ConfirmAppointmentRequest(null));
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task ArriveAsync(AppointmentDto? a)
    {
        if (a is null) return;
        try
        {
            await _api.ArriveAppointmentAsync(a.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task CancelAsync(AppointmentDto? a)
    {
        if (a is null) return;
        if (MessageBox.Show($"确认取消 {a.CustomerName} 的预约？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
        try
        {
            await _api.CancelAppointmentAsync(a.Id, new CancelAppointmentRequest("门店端取消"));
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
