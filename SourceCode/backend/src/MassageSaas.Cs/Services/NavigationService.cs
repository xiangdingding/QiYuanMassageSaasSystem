using CommunityToolkit.Mvvm.ComponentModel;

namespace MassageSaas.Cs.Services;

public partial class NavigationService : ObservableObject
{
    [ObservableProperty]
    private object? currentViewModel;

    public void NavigateTo(object viewModel) => CurrentViewModel = viewModel;
}
