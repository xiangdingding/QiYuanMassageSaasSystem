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

    public ReviewsViewModel(IApiClient api)
    {
        _api = api;
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

    [RelayCommand]
    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            var from = DateTime.UtcNow.Date.AddDays(-Math.Max(1, Days));
            int? rating = RatingFilter is >= 1 and <= 5 ? RatingFilter : null;
            Rows = new ObservableCollection<ServiceReviewDto>(
                await _api.GetReviewsAsync(rating: rating, from: from));
            Summary = new ObservableCollection<TechnicianReviewSummaryDto>(
                await _api.GetReviewSummaryAsync(from: from));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }
}
