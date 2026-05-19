namespace MassageSaas.Shared.Reports;

/// <summary>会员分析：按最近消费时间分层（活跃/沉睡/流失），含复购率。</summary>
public record MemberAnalysisDto(
    long StoreId,
    int TotalMembers,
    int NeverConsumed,
    int ActiveMembers,        // 30 天内有消费
    int DormantMembers,       // 31-90 天
    int LostMembers,          // 超过 90 天
    int NewMembersThisMonth,
    int RepeatMembers,        // 累计 ≥2 笔已完成订单
    decimal RepeatRate);      // 复购率 = 复购会员 / 有过消费的会员（百分比 0-100）

/// <summary>某服务在某月的钟数。</summary>
public record ServiceTrendMonthDto(int Year, int Month, int Rounds);

/// <summary>某服务的热度趋势：总钟数 + 逐月钟数。</summary>
public record ServiceTrendDto(
    long ServiceId,
    string ServiceName,
    int TotalRounds,
    IReadOnlyList<ServiceTrendMonthDto> Months);

/// <summary>服务热度趋势：近 N 个月热门服务的逐月钟数。</summary>
public record ServicePopularityTrendDto(
    int Months,
    IReadOnlyList<ServiceTrendDto> Services);

/// <summary>技师质量：钟数、投诉数与投诉率。</summary>
public record TechnicianQualityDto(
    long TechnicianId,
    string TechnicianName,
    int? EmployeeNo,
    int RoundCount,
    int ComplaintCount,
    decimal ComplaintRate);   // 投诉率 = 投诉数 / 钟数（百分比 0-100）
