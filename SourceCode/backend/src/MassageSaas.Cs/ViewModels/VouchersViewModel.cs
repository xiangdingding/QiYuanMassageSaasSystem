using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Vouchers;

namespace MassageSaas.Cs.ViewModels;

/// <summary>优惠券：团购券（美团/点评）与门店券的录入、查看与作废。核销在收银台完成。</summary>
public partial class VouchersViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public VouchersViewModel(IApiClient api)
    {
        _api = api;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<SelectableVoucher> rows = new();

    /// <summary>状态筛选：0 全部，1 未核销，2 已核销，3 已过期，4 已作废。</summary>
    [ObservableProperty]
    private int statusFilter = 1;

    /// <summary>类型筛选：0 全部类型，1 店内券，2 团购券。</summary>
    [ObservableProperty]
    private int kindFilter = 0;

    [ObservableProperty]
    private string keyword = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private int page = 1;

    [ObservableProperty]
    private int pageSize = 20;

    [ObservableProperty]
    private int total = 0;

    /// <summary>每页条数下拉选项；ComboBox.SelectedItem 直接绑 PageSize（int）。</summary>
    public int[] PageSizeOptions { get; } = { 20, 50, 100, 200 };

    /// <summary>当前页码上界，给 View 的"下一页"按钮判 IsEnabled 用。</summary>
    public int LastPage => Math.Max(1, (Total + PageSize - 1) / PageSize);

    /// <summary>"X / Y" 显示文本。</summary>
    public string PageDisplay => $"第 {Page} / {LastPage} 页 · 共 {Total} 条";

    public bool CanGoPrev => Page > 1;
    public bool CanGoNext => Page < LastPage;

    partial void OnStatusFilterChanged(int value)
    {
        Page = 1;
        _ = ReloadAsync();
    }

    partial void OnKindFilterChanged(int value)
    {
        Page = 1;
        _ = ReloadAsync();
    }

    partial void OnTotalChanged(int value)
    {
        OnPropertyChanged(nameof(LastPage));
        OnPropertyChanged(nameof(PageDisplay));
        OnPropertyChanged(nameof(CanGoNext));
    }

    partial void OnPageChanged(int value)
    {
        OnPropertyChanged(nameof(PageDisplay));
        OnPropertyChanged(nameof(CanGoPrev));
        OnPropertyChanged(nameof(CanGoNext));
    }

    partial void OnPageSizeChanged(int value)
    {
        OnPropertyChanged(nameof(LastPage));
        OnPropertyChanged(nameof(PageDisplay));
        OnPropertyChanged(nameof(CanGoNext));
        // 回到第 1 页；reload 交由 PaginationBar 的 PageChangedCommand 触发，避免重复请求
        Page = 1;
    }

    [RelayCommand]
    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            var status = StatusFilter switch
            {
                1 => "Active",
                2 => "Redeemed",
                3 => "Expired",
                4 => "Cancelled",
                _ => null
            };
            var kind = KindFilter switch
            {
                1 => "StoreCoupon",
                2 => "GroupBuy",
                _ => null
            };
            var resp = await _api.GetVouchersAsync(
                status,
                kind,
                string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim(),
                Page, PageSize);
            Rows = new ObservableCollection<SelectableVoucher>(resp.Items.Select(d => new SelectableVoucher(d)));
            Total = resp.Total;
            // 服务端 SafePage 可能修正过当前页，回填以便分页显示一致
            if (resp.Page != Page) Page = resp.Page;
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task PrevPageAsync()
    {
        if (!CanGoPrev) return;
        Page--;
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (!CanGoNext) return;
        Page++;
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        // 搜索/关键字改变后回到第 1 页
        Page = 1;
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var dlg = new Views.VoucherFormWindow();
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateVoucherAsync(dlg.BuildRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>批量生成同规格券：弹窗自己持有 IApiClient 直接发请求 + 展示生成码。</summary>
    [RelayCommand]
    private async Task BatchCreateAsync()
    {
        var dlg = new Views.BatchVoucherWindow(_api);
        var result = dlg.ShowDialog();
        // DialogResult=true 表示本次会话生成过至少一批，需要刷新列表
        if (result == true) await ReloadAsync();
    }

    [RelayCommand]
    private async Task CancelAsync(SelectableVoucher? v)
    {
        if (v is null) return;
        if (v.Status != "Active") { MessageBox.Show("仅未核销的券可作废"); return; }
        if (MessageBox.Show($"作废券 {v.Code}（{v.Title}）？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.CancelVoucherAsync(v.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>跳过原因摘要：取前 3 个拼起来给收银员看，整批数量在尾巴交代。</summary>
    private static string SummarizeSkipped(IReadOnlyList<BulkVoucherSkip> skipped)
    {
        if (skipped.Count == 0) return string.Empty;
        var sample = string.Join("、",
            skipped.Take(3).Select(s => $"{s.Code ?? $"id={s.Id}"}（{s.Reason}）"));
        return skipped.Count > 3 ? $"{sample} 等 {skipped.Count} 条" : sample;
    }

    [RelayCommand]
    private async Task BulkCancelAsync()
    {
        var ids = Rows.Where(r => r.IsSelected && r.Status == "Active").Select(r => r.Id).ToList();
        if (ids.Count == 0)
        {
            MessageBox.Show("请勾选状态为「生效中」的券。\n已核销、已作废、已过期的券不可作废。",
                "批量作废", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show($"确认批量作废 {ids.Count} 张券？", "批量作废",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            var resp = await _api.BulkCancelVouchersAsync(new BulkVoucherActionRequest(ids));
            var msg = $"已作废 {resp.Affected} 张";
            if (resp.Skipped.Count > 0) msg += $"，跳过 {resp.Skipped.Count} 张：{SummarizeSkipped(resp.Skipped)}";
            MessageBox.Show(msg, "批量作废", MessageBoxButton.OK, MessageBoxImage.Information);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task BulkDeleteAsync()
    {
        var ids = Rows.Where(r => r.IsSelected && r.Status == "Cancelled").Select(r => r.Id).ToList();
        if (ids.Count == 0)
        {
            MessageBox.Show("请勾选状态为「已作废」的券。\n只有已作废的券才能物理删除；其它请先作废再删除。",
                "批量删除", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show($"确认永久删除 {ids.Count} 张已作废券？\n删除后无法恢复。",
                "批量删除", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            var resp = await _api.BulkDeleteVouchersAsync(new BulkVoucherActionRequest(ids));
            var msg = $"已删除 {resp.Affected} 张";
            if (resp.Skipped.Count > 0) msg += $"，跳过 {resp.Skipped.Count} 张：{SummarizeSkipped(resp.Skipped)}";
            MessageBox.Show(msg, "批量删除", MessageBoxButton.OK, MessageBoxImage.Information);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
