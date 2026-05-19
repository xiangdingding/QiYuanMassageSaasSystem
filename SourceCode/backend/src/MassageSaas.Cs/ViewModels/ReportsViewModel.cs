using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Reports;

namespace MassageSaas.Cs.ViewModels;

/// <summary>服务热度趋势在表格里的一行：服务名 + 总钟数 + 逐月文字。</summary>
public record ServiceTrendRow(string ServiceName, int TotalRounds, string MonthsText);

/// <summary>报表中心：日报/技师业绩/月报/年报/服务热度/客流/会员分析/服务趋势/技师质量。</summary>
public partial class ReportsViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public ReportsViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    // 日报 + 技师业绩
    [ObservableProperty] private DateTime date = DateTime.Today;
    [ObservableProperty] private DateTime fromDate = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime toDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private DailyReportDto? daily;
    [ObservableProperty] private ObservableCollection<TechnicianPerformanceDto> performance = new();

    // 月报 / 年报（DatePicker 取该月/该年任意一天）
    [ObservableProperty] private DateTime monthlyMonth = DateTime.Today;
    [ObservableProperty] private MonthlyReportDto? monthly;
    [ObservableProperty] private DateTime yearlyYear = DateTime.Today;
    [ObservableProperty] private YearlyReportDto? yearly;

    // 区间报表（服务热度 / 客流 / 技师质量 共用一个时间窗）
    [ObservableProperty] private DateTime rangeFrom = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime rangeTo = DateTime.Today.AddDays(1);
    [ObservableProperty] private ObservableCollection<ServicePopularityDto> popularity = new();
    [ObservableProperty] private ObservableCollection<CustomerFlowPointDto> customerFlow = new();
    [ObservableProperty] private ObservableCollection<TechnicianQualityDto> quality = new();

    // 会员分析 / 服务趋势
    [ObservableProperty] private MemberAnalysisDto? memberAnalysis;
    [ObservableProperty] private int trendMonths = 6;
    [ObservableProperty] private ObservableCollection<ServiceTrendRow> serviceTrendRows = new();

    [ObservableProperty] private bool isBusy;

    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        try
        {
            Daily = await _api.GetDailyReportAsync(sid, Date);
            Performance = new ObservableCollection<TechnicianPerformanceDto>(
                await _api.GetTechnicianPerformanceAsync(sid, FromDate, ToDate));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task LoadMonthlyAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        try { Monthly = await _api.GetMonthlyReportAsync(sid, MonthlyMonth.Year, MonthlyMonth.Month); }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task LoadYearlyAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        try { Yearly = await _api.GetYearlyReportAsync(sid, YearlyYear.Year); }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task LoadPopularityAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        try
        {
            Popularity = new ObservableCollection<ServicePopularityDto>(
                await _api.GetServicePopularityAsync(sid, RangeFrom, RangeTo));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task LoadFlowAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        try
        {
            CustomerFlow = new ObservableCollection<CustomerFlowPointDto>(
                await _api.GetCustomerFlowAsync(sid, RangeFrom, RangeTo));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task LoadMemberAnalysisAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        try { MemberAnalysis = await _api.GetMemberAnalysisAsync(sid); }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task LoadServiceTrendAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        try
        {
            var trend = await _api.GetServiceTrendAsync(sid, TrendMonths);
            ServiceTrendRows = new ObservableCollection<ServiceTrendRow>(
                trend.Services.Select(s => new ServiceTrendRow(
                    s.ServiceName, s.TotalRounds,
                    string.Join("　", s.Months.Select(m => $"{m.Month}月:{m.Rounds}")))));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task LoadQualityAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        try
        {
            Quality = new ObservableCollection<TechnicianQualityDto>(
                await _api.GetTechnicianQualityAsync(sid, RangeFrom, RangeTo));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
