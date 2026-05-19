using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Inventory;

namespace MassageSaas.Cs.ViewModels;

/// <summary>物耗库存：精油 / 毛巾 / 足浴包等耗材的台账、出入库流水与低库存预警。</summary>
public partial class InventoryViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public InventoryViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<InventoryItemDto> rows = new();

    [ObservableProperty]
    private ObservableCollection<InventoryMovementDto> movements = new();

    [ObservableProperty]
    private InventoryItemDto? selected;

    [ObservableProperty]
    private bool onlyLowStock;

    [ObservableProperty]
    private bool isBusy;

    partial void OnOnlyLowStockChanged(bool value) => _ = ReloadAsync();

    partial void OnSelectedChanged(InventoryItemDto? value) => _ = LoadMovementsAsync();

    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        try
        {
            Rows = new ObservableCollection<InventoryItemDto>(
                await _api.GetInventoryAsync(sid, OnlyLowStock));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    private async Task LoadMovementsAsync()
    {
        if (Selected is null) { Movements = new(); return; }
        try
        {
            Movements = new ObservableCollection<InventoryMovementDto>(
                await _api.GetInventoryMovementsAsync(Selected.Id));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (_context.ActiveStoreId is not long sid) { MessageBox.Show("未选择门店"); return; }
        var dlg = new Views.InventoryFormWindow(null, sid);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateInventoryItemAsync(dlg.BuildCreateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task EditAsync(InventoryItemDto? item)
    {
        if (item is null) return;
        var dlg = new Views.InventoryFormWindow(item, item.StoreId);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.UpdateInventoryItemAsync(item.Id, dlg.BuildUpdateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task MoveAsync(InventoryItemDto? item)
    {
        if (item is null) { MessageBox.Show("请先选择一项耗材"); return; }
        var dlg = new Views.InventoryMovementWindow(item.Name);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateInventoryMovementAsync(dlg.BuildRequest(item.Id));
            await ReloadAsync();
            if (Selected?.Id == item.Id) await LoadMovementsAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
