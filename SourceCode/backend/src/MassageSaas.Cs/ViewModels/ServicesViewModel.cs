using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Services;

namespace MassageSaas.Cs.ViewModels;

public partial class ServicesViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public ServicesViewModel(IApiClient api)
    {
        _api = api;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<ServiceItemDto> rows = new();

    [ObservableProperty]
    private bool includeInactive;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private ServiceItemDto? selected;

    [RelayCommand]
    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            Rows = new ObservableCollection<ServiceItemDto>(await _api.GetServicesAsync(IncludeInactive));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    partial void OnIncludeInactiveChanged(bool value) => _ = ReloadAsync();

    [RelayCommand]
    private async Task CreateAsync()
    {
        var dlg = new Views.ServiceFormWindow(null);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateServiceAsync(dlg.BuildCreateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task EditAsync(ServiceItemDto? s)
    {
        if (s is null) return;
        var dlg = new Views.ServiceFormWindow(s);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.UpdateServiceAsync(s.Id, dlg.BuildUpdateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task RemoveAsync(ServiceItemDto? s)
    {
        if (s is null) return;
        if (MessageBox.Show($"确认删除 {s.Name}？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning)
            != MessageBoxResult.OK) return;
        try
        {
            await _api.DeleteServiceAsync(s.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
