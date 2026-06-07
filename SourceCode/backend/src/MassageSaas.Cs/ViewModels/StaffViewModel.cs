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

    /// <summary>重置时批量改筛选条件，用此标志压制每次属性变化都触发的自动查询，避免重复请求。</summary>
    private bool _suppressAutoReload;

    /// <summary>角色下拉框选值即查（回到第 1 页，对齐 BS）。</summary>
    partial void OnRoleFilterChanged(string? value)
    {
        if (_suppressAutoReload) return;
        Page = 1;
        _ = ReloadAsync();
    }

    // ---- 分页（对齐 BS） ----
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageInfo))]
    [NotifyPropertyChangedFor(nameof(CanPrev))]
    [NotifyPropertyChangedFor(nameof(CanNext))]
    private int page = 1;

    [ObservableProperty]
    private int pageSize = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageInfo))]
    [NotifyPropertyChangedFor(nameof(CanNext))]
    private int total;

    public int TotalPages => Total <= 0 ? 1 : (Total + PageSize - 1) / PageSize;
    public string PageInfo => $"第 {Page} / {TotalPages} 页 · 共 {Total} 人";
    public bool CanPrev => Page > 1;
    public bool CanNext => Page < TotalPages;

    [RelayCommand]
    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            var data = await _api.GetStaffAsync(
                page: Page, pageSize: PageSize,
                role: string.IsNullOrEmpty(RoleFilter) ? null : RoleFilter,
                storeId: _context.ActiveStoreId,
                keyword: string.IsNullOrWhiteSpace(Keyword) ? null : Keyword);
            Rows = new ObservableCollection<StaffDto>(data.Items);
            Total = data.Total;
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    /// <summary>查询：回到第 1 页（对齐 BS）。</summary>
    [RelayCommand]
    private async Task Search()
    {
        Page = 1;
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task ResetQuery()
    {
        _suppressAutoReload = true;
        Keyword = string.Empty;
        RoleFilter = null;
        Page = 1;
        _suppressAutoReload = false;
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task PrevPage()
    {
        if (!CanPrev) return;
        Page--;
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (!CanNext) return;
        Page++;
        await ReloadAsync();
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

    /// <summary>跨店调动（永久/临时借调）+ 历史归还。逻辑与 BS 端 openTransfer 一致。</summary>
    [RelayCommand]
    private async Task TransferAsync(StaffDto? s)
    {
        if (s is null) return;
        var dlg = new Views.StaffTransferWindow(_api, s, _context.Stores) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() == true) await ReloadAsync();
    }
}
