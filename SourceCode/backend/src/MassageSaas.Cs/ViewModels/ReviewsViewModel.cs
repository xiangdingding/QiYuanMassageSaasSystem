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

    partial void OnRatingFilterChanged(int value) => _ = ReloadAsync();
    partial void OnDaysChanged(int value) => _ = ReloadAsync();

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
            Rows = new ObservableCollection<ServiceReviewDto>(
                (await _api.GetReviewsAsync(rating: rating, from: from, pageSize: 200)).Items);
            Summary = new ObservableCollection<TechnicianReviewSummaryDto>(
                await _api.GetReviewSummaryAsync(from: from));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }
}
