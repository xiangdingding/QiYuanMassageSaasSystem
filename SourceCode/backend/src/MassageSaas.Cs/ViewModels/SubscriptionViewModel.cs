using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Subscriptions;

namespace MassageSaas.Cs.ViewModels;

public partial class SubscriptionViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public SubscriptionViewModel(IApiClient api)
    {
        _api = api;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private TenantSubscriptionStatusDto? status;

    [ObservableProperty]
    private ObservableCollection<SubscriptionDto> history = new();

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            Status = await _api.GetMySubscriptionAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }
}
