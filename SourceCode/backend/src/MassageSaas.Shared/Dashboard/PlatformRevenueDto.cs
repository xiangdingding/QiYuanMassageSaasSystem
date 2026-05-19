namespace MassageSaas.Shared.Dashboard;

/// <summary>平台方营收报表：订阅付费的月度趋势与多维拆分。仅统计已支付订单。</summary>
public record PlatformRevenueDto(
    int Months,
    decimal TotalAmount,
    int TotalOrders,
    decimal NewCustomerAmount,
    decimal RenewalAmount,
    IReadOnlyList<RevenueMonthDto> MonthlyTrend,
    IReadOnlyList<RevenueBreakdownDto> ByPlan,
    IReadOnlyList<RevenueBreakdownDto> ByChannel);

/// <summary>某月营收。</summary>
public record RevenueMonthDto(int Year, int Month, decimal Amount, int OrderCount);

/// <summary>按某维度（套餐 / 支付渠道）聚合的营收。</summary>
public record RevenueBreakdownDto(string Name, decimal Amount, int OrderCount);
