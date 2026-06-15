using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
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
        // 窗口自己调接口：仅当成功（DialogResult=true）才刷新；异常时窗口保持打开
        var dlg = new Views.StoreFormWindow(_api, null, Headquarters())
        { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true) return;
        MessageBox.Show("已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task EditAsync(StoreDto? s)
    {
        if (s is null) return;
        var dlg = new Views.StoreFormWindow(_api, s, Headquarters())
        { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true) return;
        MessageBox.Show("已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        await ReloadAsync();
    }

    /// <summary>当前所有总店（新建分店时选「所属总店」用）。</summary>
    private List<StoreDto> Headquarters() => Rows.Where(r => r.IsHeadquarters).ToList();
}
