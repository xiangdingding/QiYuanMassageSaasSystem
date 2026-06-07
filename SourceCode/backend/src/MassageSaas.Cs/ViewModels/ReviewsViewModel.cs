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

    /// <summary>评分筛选：0 = 全部，1-5 = 指定星级。</summary>
    [ObservableProperty]
    private int ratingFilter;

    [ObservableProperty]
    private int days = 30;

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
    partial void OnDaysChanged(int value) { Page = 1; _ = ReloadAsync(); }

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
            var from = DateTime.UtcNow.Date.AddDays(-Math.Max(1, Days));
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
            var paged = await _api.GetReviewsAsync(rating: rating, from: from, page: Page, pageSize: PageSize);
            Rows = new ObservableCollection<ServiceReviewDto>(paged.Items);
            Total = paged.Total;
            Summary = new ObservableCollection<TechnicianReviewSummaryDto>(
                await _api.GetReviewSummaryAsync(from: from));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }
}
