using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.DayCloses;

namespace MassageSaas.Cs.ViewModels;

public partial class DayCloseViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public DayCloseViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = LoadAsync();
    }

    [ObservableProperty]
    private DateTime businessDate = DateTime.Today;

    [ObservableProperty]
    private DayClosePreviewDto? preview;

    [ObservableProperty]
    private decimal actualCash;

    [ObservableProperty]
    private string? remark;

    [ObservableProperty]
    private ObservableCollection<DayCloseDto> history = new();

    [ObservableProperty]
    private bool isBusy;

    public decimal Variance => ActualCash - (Preview?.ExpectedCash ?? 0m);

    partial void OnPreviewChanged(DayClosePreviewDto? value)
    {
        if (value is not null) ActualCash = value.ExpectedCash;
        OnPropertyChanged(nameof(Variance));
    }

    partial void OnActualCashChanged(decimal value) => OnPropertyChanged(nameof(Variance));

    partial void OnBusinessDateChanged(DateTime value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        try
        {
            Preview = await _api.GetDayClosePreviewAsync(sid, BusinessDate);
            History = new ObservableCollection<DayCloseDto>(await _api.GetDayCloseHistoryAsync(sid));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (_context.ActiveStoreId is not long sid || Preview is null) return;
        if (Preview.AlreadyClosed) { MessageBox.Show("该日已日结"); return; }
        if (Math.Abs(Variance) > 0.005m)
        {
            if (MessageBox.Show($"差额 ¥{Variance:F2}，确认提交？", "差额确认",
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        }
        IsBusy = true;
        try
        {
            await _api.SubmitDayCloseAsync(new SubmitDayCloseRequest(sid, BusinessDate, ActualCash, Remark));
            MessageBox.Show("日结已提交");
            await LoadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }
}
