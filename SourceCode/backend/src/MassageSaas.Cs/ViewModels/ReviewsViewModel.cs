using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Reviews;

namespace MassageSaas.Cs.ViewModels;

/// <summary>服务评价：技师评分明细 + 按技师的均分汇总。只读。</summary>
public partial class ReviewsViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public ReviewsViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<ServiceReviewDto> rows = new();

    [ObservableProperty]
    private ObservableCollection<TechnicianReviewSummaryDto> summary = new();

    /// <summary>技师筛选下拉项；第一项 Id=0 表示全部技师。</summary>
    [ObservableProperty]
    private ObservableCollection<TechFilterOption> techOptions = new();

    /// <summary>当前选中的技师筛选：0 = 全部。</summary>
    [ObservableProperty]
    private long technicianFilter;

    /// <summary>评分筛选：0 = 全部，1-5 = 指定星级。</summary>
    [ObservableProperty]
    private int ratingFilter;

    /// <summary>开始日期（含）；null = 不限。</summary>
    [ObservableProperty]
    private DateTime? fromDate;

    /// <summary>结束日期（含当天）；null = 不限。</summary>
    [ObservableProperty]
    private DateTime? toDate;

    [ObservableProperty]
    private bool isBusy;

    // ---- 分页（仅针对评分明细 Rows；技师均分汇总 Summary 不分页） ----
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
    public string PageInfo => $"第 {Page} / {TotalPages} 页 · 共 {Total} 条";
    public bool CanPrev => Page > 1;
    public bool CanNext => Page < TotalPages;

    partial void OnRatingFilterChanged(int value) { Page = 1; _ = ReloadAsync(); }
    partial void OnTechnicianFilterChanged(long value) { Page = 1; _ = ReloadAsync(); }
    partial void OnFromDateChanged(DateTime? value) { Page = 1; _ = ReloadAsync(); }
    partial void OnToDateChanged(DateTime? value) { Page = 1; _ = ReloadAsync(); }

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

    /// <summary>代客录入评价：弹窗自带 IApiClient，提交成功后刷新列表。</summary>
    [RelayCommand]
    private async Task CreateAsync()
    {
        if (_context.ActiveStoreId is not long sid)
        {
            System.Windows.MessageBox.Show("请先选择门店");
            return;
        }
        var dlg = new Views.ReviewCreateWindow(_api, sid);
        if (dlg.ShowDialog() == true) await ReloadAsync();
    }

    [RelayCommand]
    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            await EnsureTechOptionsAsync();
            // 与 BS 端一致：开始/结束日期任选其一或都填；结束日期含当天（+1 天作上界）
            DateTime? from = FromDate?.Date;
            DateTime? to = ToDate?.Date.AddDays(1);
            // 下拉按"非常满意→非常不满意"排列（索引 1-5），映射到评分 5-1
            int? rating = RatingFilter switch
            {
                1 => 5,
                2 => 4,
                3 => 3,
                4 => 2,
                5 => 1,
                _ => null
            };
            long? tech = TechnicianFilter > 0 ? TechnicianFilter : null;
            var paged = await _api.GetReviewsAsync(technicianId: tech, rating: rating, from: from, to: to, page: Page, pageSize: PageSize);
            Rows = new ObservableCollection<ServiceReviewDto>(paged.Items);
            Total = paged.Total;
            Summary = new ObservableCollection<TechnicianReviewSummaryDto>(
                await _api.GetReviewSummaryAsync(from: from, to: to));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    /// <summary>首次加载技师下拉项（含"全部技师"占位）。门店未选定时跳过，等下次刷新再补。</summary>
    private async Task EnsureTechOptionsAsync()
    {
        if (TechOptions.Count > 0) return;
        if (_context.ActiveStoreId is not long sid) return;
        var resp = await _api.GetStaffAsync(role: "Technician", storeId: sid, page: 1, pageSize: 200);
        var list = new ObservableCollection<TechFilterOption> { new(0, "全部技师") };
        foreach (var s in resp.Items)
            list.Add(new TechFilterOption(s.Id, $"{s.RealName ?? s.Username}（工号 {(s.EmployeeNo?.ToString() ?? "—")}）"));
        TechOptions = list;
    }
}

/// <summary>技师筛选下拉项。Id=0 表示全部技师。</summary>
public record TechFilterOption(long Id, string Display)
{
    // 下拉框选中区在自定义模板下会回退到 ToString，记录默认 ToString 是 JSON 形式，这里改成纯文本
    public override string ToString() => Display;
}
