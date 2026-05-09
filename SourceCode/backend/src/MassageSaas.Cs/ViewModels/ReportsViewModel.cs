using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Reports;

namespace MassageSaas.Cs.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public ReportsViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private DateTime date = DateTime.Today;

    [ObservableProperty]
    private DateTime fromDate = DateTime.Today.AddDays(-7);

    [ObservableProperty]
    private DateTime toDate = DateTime.Today.AddDays(1);

    [ObservableProperty]
    private DailyReportDto? daily;

    [ObservableProperty]
    private ObservableCollection<TechnicianPerformanceDto> performance = new();

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        try
        {
            Daily = await _api.GetDailyReportAsync(sid, Date);
            Performance = new ObservableCollection<TechnicianPerformanceDto>(
                await _api.GetTechnicianPerformanceAsync(sid, FromDate, ToDate));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }
}
