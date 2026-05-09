using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Rooms;

namespace MassageSaas.Cs.ViewModels;

public partial class RoomsViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public RoomsViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<RoomDto> rows = new();

    [ObservableProperty]
    private bool includeInactive;

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        try
        {
            Rows = new ObservableCollection<RoomDto>(await _api.GetRoomsAsync(sid, IncludeInactive));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    partial void OnIncludeInactiveChanged(bool value) => _ = ReloadAsync();

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (_context.ActiveStoreId is null) { MessageBox.Show("未选择门店"); return; }
        var dlg = new Views.RoomFormWindow(null, _context.ActiveStoreId.Value);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateRoomAsync(dlg.BuildCreateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task EditAsync(RoomDto? r)
    {
        if (r is null) return;
        var dlg = new Views.RoomFormWindow(r, r.StoreId);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.UpdateRoomAsync(r.Id, dlg.BuildUpdateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task RemoveAsync(RoomDto? r)
    {
        if (r is null) return;
        if (r.IsOccupied) { MessageBox.Show("房间被占用，请改为停用"); return; }
        if (MessageBox.Show($"删除房间 {r.RoomNo}？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.DeleteRoomAsync(r.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
