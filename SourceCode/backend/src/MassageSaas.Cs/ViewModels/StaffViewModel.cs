using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.ViewModels;

public partial class StaffViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public StaffViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<StaffDto> rows = new();

    [ObservableProperty]
    private string? roleFilter;

    [ObservableProperty]
    private string keyword = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            var data = await _api.GetStaffAsync(
                page: 1, pageSize: 200,
                role: string.IsNullOrEmpty(RoleFilter) ? null : RoleFilter,
                storeId: _context.ActiveStoreId,
                keyword: string.IsNullOrWhiteSpace(Keyword) ? null : Keyword);
            Rows = new ObservableCollection<StaffDto>(data.Items);
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (_context.ActiveStoreId is null) { MessageBox.Show("未选择门店"); return; }
        var dlg = new Views.StaffFormWindow(null, _context.ActiveStoreId.Value);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateStaffAsync(dlg.BuildCreateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task EditAsync(StaffDto? s)
    {
        if (s is null) return;
        var dlg = new Views.StaffFormWindow(s, s.StoreId ?? _context.ActiveStoreId ?? 0);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.UpdateStaffAsync(s.Id, dlg.BuildUpdateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task ResetPasswordAsync(StaffDto? s)
    {
        if (s is null) return;
        var dlg = new Views.PasswordPromptWindow($"为 {s.RealName ?? s.Username} 重置密码");
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.ResetPasswordAsync(s.Id, new ResetPasswordRequest(dlg.NewPassword));
            MessageBox.Show("密码已重置");
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
