using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Stores;

namespace MassageSaas.Cs.ViewModels;

public partial class StoresViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public StoresViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<StoreDto> rows = new();

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            var stores = await _api.GetStoresAsync();
            Rows = new ObservableCollection<StoreDto>(stores);
            _context.Stores = stores;
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var dlg = new Views.StoreFormWindow(null);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateStoreAsync(dlg.BuildCreateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task EditAsync(StoreDto? s)
    {
        if (s is null) return;
        var dlg = new Views.StoreFormWindow(s);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.UpdateStoreAsync(s.Id, dlg.BuildUpdateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
