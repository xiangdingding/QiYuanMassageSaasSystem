using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Staff;
using MassageSaas.Shared.Stores;

namespace MassageSaas.Cs.ViewModels;

public partial class StaffViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    /// <summary>门店过滤下拉项：Id 为 null 表示"全部门店"。</summary>
    public record StoreOption(long? Id, string Label);

    public StaffViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;

        var opts = new ObservableCollection<StoreOption> { new(null, "全部门店") };
        foreach (var s in _context.Stores) opts.Add(new StoreOption(s.Id, s.Name));
        StoreOptions = opts;

        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<StaffRowViewModel> rows = new();

    [ObservableProperty]
    private string? roleFilter;

    /// <summary>门店过滤下拉项（全部门店 + 各门店）。</summary>
    [ObservableProperty]
    private ObservableCollection<StoreOption> storeOptions = new();

    /// <summary>选中的过滤门店 Id；null = 全部门店（对齐 BS 的门店下拉）。</summary>
    [ObservableProperty]
    private long? storeFilter;

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

    /// <summary>门店下拉选值即查（回到第 1 页，对齐 BS）。</summary>
    partial void OnStoreFilterChanged(long? value)
    {
        if (_suppressAutoReload) return;
        Page = 1;
        _ = ReloadAsync();
    }

    private string StoreNameOf(long? storeId)
    {
        if (storeId is not long id) return "—";
        var s = _context.Stores.FirstOrDefault(x => x.Id == id);
        return s?.Name ?? $"#{id}";
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
                storeId: StoreFilter,
                keyword: string.IsNullOrWhiteSpace(Keyword) ? null : Keyword);
            Rows = new ObservableCollection<StaffRowViewModel>(
                data.Items.Select(s => new StaffRowViewModel(s, StoreNameOf(s.StoreId))));
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
        StoreFilter = null;
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
        var defaultStore = _context.ActiveStoreId ?? _context.Stores.FirstOrDefault()?.Id ?? 0;
        var nextEmployeeNo = await NextEmployeeNoAsync();
        // 窗口自己调接口：仅当真正添加成功（DialogResult=true）才会走到这里刷新；异常时窗口保持打开
        var dlg = new Views.StaffFormWindow(_api, null, defaultStore, _context.Stores, nextEmployeeNo)
        { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true) return;
        MessageBox.Show("已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        await ReloadAsync();
    }

    /// <summary>取「现有最大工号 + 1」作为新建默认工号；无任何工号时从 1 开始。</summary>
    private async Task<int> NextEmployeeNoAsync()
    {
        try
        {
            var all = await _api.GetStaffAsync(page: 1, pageSize: 1000, storeId: null);
            var max = all.Items
                .Where(s => s.EmployeeNo.HasValue)
                .Select(s => s.EmployeeNo!.Value)
                .DefaultIfEmpty(0)
                .Max();
            return max + 1;
        }
        catch { return 1; }
    }

    [RelayCommand]
    private async Task EditAsync(StaffDto? s)
    {
        if (s is null) return;
        var dlg = new Views.StaffFormWindow(_api, s, s.StoreId ?? _context.ActiveStoreId ?? 0, _context.Stores)
        { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true) return;
        MessageBox.Show("已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        await ReloadAsync();
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
