using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MassageSaas.Cs.Services;
using SkiaSharp;

namespace MassageSaas.Cs.ViewModels;

/// <summary>
/// 经营数据看板：把现有报表接口（日报/月报/会员分析/技师业绩/服务热度/客流）
/// 组合成图形化总览。指标卡用文字（读屏可达），趋势/占比/排行用 LiveCharts2 图表。
/// 明细钻取仍在「日报与业绩」报表模块。
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    // 品牌绿 + 渐变同色系调色板，与 BS 端看板一致
    private static readonly SKColor Brand = SKColor.Parse("#2D6A4F");
    private static readonly SKColor[] Palette =
    {
        SKColor.Parse("#2D6A4F"), SKColor.Parse("#40916C"), SKColor.Parse("#52B788"),
        SKColor.Parse("#74C69D"), SKColor.Parse("#95D5B2"), SKColor.Parse("#B7E4C7"),
        SKColor.Parse("#D8A657"), SKColor.Parse("#E07A5F")
    };

    public DashboardViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    // ---------- 关键指标卡 ----------
    [ObservableProperty] private decimal todayRevenue;
    [ObservableProperty] private int todayOrderCount;
    [ObservableProperty] private decimal todayRecharge;
    [ObservableProperty] private int todayRechargeCount;
    [ObservableProperty] private decimal monthRevenue;
    [ObservableProperty] private decimal monthAverageOrder;
    [ObservableProperty] private int monthRounds;
    [ObservableProperty] private int monthOrderCount;
    [ObservableProperty] private int memberTotal;
    [ObservableProperty] private int newMembers;
    [ObservableProperty] private decimal repeatRate;
    [ObservableProperty] private int repeatMembers;

    // ---------- 图表：序列 + 坐标轴 ----------
    [ObservableProperty] private ISeries[] revenueSeries = System.Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] revenueXAxes = { NewCategoryAxis() };
    [ObservableProperty] private Axis[] revenueYAxes = NewValueAxes();
    [ObservableProperty] private ISeries[] paySeries = System.Array.Empty<ISeries>();
    [ObservableProperty] private ISeries[] techSeries = System.Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] techXAxes = { NewCategoryAxis() };
    [ObservableProperty] private ISeries[] popularitySeries = System.Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] popularityYAxes = { NewCategoryAxis() };
    [ObservableProperty] private ISeries[] flowSeries = System.Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] flowXAxes = { NewCategoryAxis() };
    [ObservableProperty] private ISeries[] memberSeries = System.Array.Empty<ISeries>();

    [ObservableProperty] private bool isBusy;

    public string ActiveStoreName => _context.ActiveStore?.Name ?? "本店";

    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        OnPropertyChanged(nameof(ActiveStoreName));
        try
        {
            var from = DateTime.Today.AddDays(-29);
            var to = DateTime.Today.AddDays(1);

            var daily = await _api.GetDailyReportAsync(sid);
            var monthly = await _api.GetMonthlyReportAsync(sid);
            var member = await _api.GetMemberAnalysisAsync(sid);
            var perf = await _api.GetTechnicianPerformanceAsync(sid, from, to);
            var popularity = await _api.GetServicePopularityAsync(sid, from, to);
            var flow = await _api.GetCustomerFlowAsync(sid, from, to);

            // KPI
            TodayRevenue = daily.Revenue;
            TodayOrderCount = daily.OrderCount;
            TodayRecharge = daily.MemberRechargeAmount;
            TodayRechargeCount = daily.MemberRechargeCount;
            MonthRevenue = monthly.Revenue;
            MonthAverageOrder = monthly.AverageOrder;
            MonthRounds = monthly.RoundsCount;
            MonthOrderCount = monthly.OrderCount;
            MemberTotal = member.TotalMembers;
            NewMembers = member.NewMembersThisMonth;
            RepeatRate = member.RepeatRate;
            RepeatMembers = member.RepeatMembers;

            BuildRevenueTrend(monthly);
            BuildPayPie(daily);
            BuildTechPerf(perf);
            BuildPopularity(popularity);
            BuildFlow(flow);
            BuildMemberPie(member);
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    // ---------- 本月每日营业额 + 钟数（双折线） ----------
    private void BuildRevenueTrend(Shared.Reports.MonthlyReportDto monthly)
    {
        var rows = monthly.Daily;
        RevenueSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "营业额",
                Values = rows.Select(r => (double)r.Revenue).ToArray(),
                Stroke = new SolidColorPaint(Brand, 2),
                GeometryStroke = new SolidColorPaint(Brand, 2),
                GeometrySize = 6,
                Fill = new SolidColorPaint(Brand.WithAlpha(36))
            },
            new LineSeries<double>
            {
                Name = "钟数",
                Values = rows.Select(r => (double)r.Rounds).ToArray(),
                Stroke = new SolidColorPaint(Palette[2], 2),
                GeometryStroke = new SolidColorPaint(Palette[2], 2),
                GeometrySize = 6,
                Fill = null,
                ScalesYAt = 1
            }
        };
        RevenueXAxes = new[] { NewCategoryAxis(rows.Select(r => r.Day.ToString("MM-dd"))) };
        RevenueYAxes = NewValueAxes();
    }

    // ---------- 今日支付方式占比（饼图） ----------
    private void BuildPayPie(Shared.Reports.DailyReportDto d)
    {
        var slices = new (string Label, decimal Value)[]
        {
            ("现金", d.CashAmount), ("会员卡", d.MemberCardAmount), ("微信", d.WechatAmount),
            ("支付宝", d.AlipayAmount), ("银行卡", d.BankCardAmount)
        }.Where(x => x.Value > 0).ToArray();

        PaySeries = slices.Select((x, i) => (ISeries)new PieSeries<double>
        {
            Name = x.Label,
            Values = new[] { (double)x.Value },
            Fill = new SolidColorPaint(Palette[i % Palette.Length]),
            DataLabelsPaint = new SolidColorPaint(SKColors.White),
            DataLabelsFormatter = p => $"{x.Label} {p.StackedValue!.Share:P0}"
        }).ToArray();
    }

    // ---------- 技师业绩排行（近 30 天提成，柱状） ----------
    private void BuildTechPerf(IReadOnlyList<Shared.Reports.TechnicianPerformanceDto> perf)
    {
        var rows = perf.OrderByDescending(p => p.TotalCommission).Take(10).ToArray();
        TechSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "提成",
                Values = rows.Select(r => (double)r.TotalCommission).ToArray(),
                Fill = new SolidColorPaint(Brand),
                MaxBarWidth = 40
            }
        };
        TechXAxes = new[] { NewCategoryAxis(rows.Select(r => r.TechnicianName)) };
    }

    // ---------- 服务热度 Top 10（近 30 天钟数，横向柱状） ----------
    private void BuildPopularity(IReadOnlyList<Shared.Reports.ServicePopularityDto> popularity)
    {
        // 横向柱：取前 10，逆序使最高在顶部
        var rows = popularity.OrderByDescending(p => p.RoundsCount).Take(10).Reverse().ToArray();
        PopularitySeries = new ISeries[]
        {
            new RowSeries<double>
            {
                Name = "钟数",
                Values = rows.Select(r => (double)r.RoundsCount).ToArray(),
                Fill = new SolidColorPaint(Palette[1]),
                MaxBarWidth = 24
            }
        };
        PopularityYAxes = new[] { NewCategoryAxis(rows.Select(r => r.ServiceName)) };
    }

    // ---------- 客流趋势（近 30 天订单数 + 唯一会员，双折线） ----------
    private void BuildFlow(IReadOnlyList<Shared.Reports.CustomerFlowPointDto> flow)
    {
        FlowSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "订单数",
                Values = flow.Select(f => (double)f.OrderCount).ToArray(),
                Stroke = new SolidColorPaint(Brand, 2),
                GeometryStroke = new SolidColorPaint(Brand, 2),
                GeometrySize = 5,
                Fill = new SolidColorPaint(Brand.WithAlpha(30))
            },
            new LineSeries<double>
            {
                Name = "唯一会员",
                Values = flow.Select(f => (double)f.UniqueMembers).ToArray(),
                Stroke = new SolidColorPaint(Palette[6], 2),
                GeometryStroke = new SolidColorPaint(Palette[6], 2),
                GeometrySize = 5,
                Fill = null
            }
        };
        FlowXAxes = new[] { NewCategoryAxis(flow.Select(f => f.Date.ToString("MM-dd"))) };
    }

    // ---------- 会员构成（饼图） ----------
    private void BuildMemberPie(Shared.Reports.MemberAnalysisDto m)
    {
        var slices = new (string Label, int Value)[]
        {
            ("活跃(30天内)", m.ActiveMembers), ("沉睡(31-90天)", m.DormantMembers),
            ("流失(>90天)", m.LostMembers), ("从未消费", m.NeverConsumed)
        }.Where(x => x.Value > 0).ToArray();

        MemberSeries = slices.Select((x, i) => (ISeries)new PieSeries<double>
        {
            Name = x.Label,
            Values = new[] { (double)x.Value },
            Fill = new SolidColorPaint(Palette[i % Palette.Length]),
            DataLabelsPaint = new SolidColorPaint(SKColors.White),
            DataLabelsFormatter = p => $"{x.Label} {p.StackedValue!.Share:P0}"
        }).ToArray();
    }

    private static Axis NewCategoryAxis(IEnumerable<string>? labels = null) => new()
    {
        Labels = labels?.ToArray() ?? System.Array.Empty<string>(),
        LabelsRotation = 0,
        TextSize = 12,
        LabelsPaint = new SolidColorPaint(SKColor.Parse("#606266"))
    };

    // 营业额趋势用双 Y 轴：左轴营业额（元），右轴钟数（计数）。
    private static Axis[] NewValueAxes() => new[]
    {
        new Axis
        {
            Name = "营业额",
            TextSize = 12,
            LabelsPaint = new SolidColorPaint(Brand),
            NamePaint = new SolidColorPaint(Brand)
        },
        new Axis
        {
            Name = "钟数",
            Position = LiveChartsCore.Measure.AxisPosition.End,
            TextSize = 12,
            ShowSeparatorLines = false,
            LabelsPaint = new SolidColorPaint(Palette[2]),
            NamePaint = new SolidColorPaint(Palette[2])
        }
    };
}
