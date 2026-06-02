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
    private readonly SessionService _session;

    public RoomsViewModel(IApiClient api, AppContextService context, SessionService session)
    {
        _api = api;
        _context = context;
        _session = session;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<RoomRowViewModel> rows = new();

    [ObservableProperty]
    private bool includeInactive;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>仅店主/店长可新建/编辑/删除/取消计时；收银员只读（与 BS 端 canManage 一致）。</summary>
    public bool CanManage => _session.Role is "ShopOwner" or "StoreManager";

    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        try
        {
            // 房间列表 + 本店进行中的计时 session 一起拉，组装成行
            var roomsTask = _api.GetRoomsAsync(sid, IncludeInactive);
            var sessionsTask = SafeGetSessionsAsync(sid);
            await Task.WhenAll(roomsTask, sessionsTask);

            var sessions = sessionsTask.Result;
            Rows = new ObservableCollection<RoomRowViewModel>(
                roomsTask.Result.Select(r =>
                    new RoomRowViewModel(r, sessions.FirstOrDefault(s => s.RoomId == r.Id && s.Status == "Open"))));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    /// <summary>计时房后端可能尚未迁移：取 session 失败时退回空列表，不影响房间管理本身。</summary>
    private async Task<List<TimedRoomSessionDto>> SafeGetSessionsAsync(long sid)
    {
        try { return await _api.GetTimedRoomSessionsAsync(sid); }
        catch { return new List<TimedRoomSessionDto>(); }
    }

    partial void OnIncludeInactiveChanged(bool value) => _ = ReloadAsync();

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (!CanManage) return;
        if (_context.ActiveStoreId is null) { MessageBox.Show("未选择门店"); return; }
        var dlg = new Views.RoomFormWindow(null, _context.ActiveStoreId.Value) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateRoomAsync(dlg.BuildCreateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task EditAsync(RoomRowViewModel? r)
    {
        if (!CanManage || r is null) return;
        var dlg = new Views.RoomFormWindow(r.Dto, r.Dto.StoreId) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.UpdateRoomAsync(r.Id, dlg.BuildUpdateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task RemoveAsync(RoomRowViewModel? r)
    {
        if (!CanManage || r is null) return;
        if (r.HasOpenSession)
        {
            MessageBox.Show($"{r.RoomNo} 号房正在计时，请先结束或取消计时再删除。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show($"删除房间 {r.RoomNo}？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.DeleteRoomAsync(r.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>取消一段进行中的计时（误开台/客人临时不消费）：标为 Cancelled，不计费、不入账。</summary>
    [RelayCommand]
    private async Task CancelTimingAsync(RoomRowViewModel? r)
    {
        if (!CanManage || r?.Session is null) return;
        if (MessageBox.Show(
                $"确认取消 {r.RoomNo} 号房当前计时？已计 {r.Session.ElapsedMinutes} 分钟将作废、不计费。",
                "取消计时", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.CancelTimedRoomAsync(r.Session.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
