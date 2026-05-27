namespace MassageSaas.Shared.Reports;

public record DailyReportDto(
    DateTime Date,
    long StoreId,
    int OrderCount,
    decimal Revenue,
    decimal CashAmount,
    decimal MemberCardAmount,
    decimal WechatAmount,
    decimal AlipayAmount,
    decimal BankCardAmount,
    int RefundCount,
    decimal RefundAmount,
    int MemberRechargeCount,
    decimal MemberRechargeAmount);

public record TechnicianPerformanceDto(
    long TechnicianId,
    string TechnicianName,
    int? EmployeeNo,
    int OrderItemCount,
    decimal TotalServiceAmount,
    decimal TotalCommission,
    int TotalDurationMinutes,
    /// <summary>点钟次数（Σ Quantity where AssignmentSource = Designation）。</summary>
    int DesignationCount = 0,
    /// <summary>轮钟次数（Σ Quantity where AssignmentSource = Rotation）。</summary>
    int RotationCount = 0,
    /// <summary>指定率 = Designation / (Designation + Rotation)；分母 0 时为 null。
    /// 历史 Unknown 行不进分母，避免老数据稀释 KPI。</summary>
    decimal? DesignationRate = null);

public record MyPerformanceDto(
    decimal TodayAmount,
    decimal TodayCommission,
    decimal MonthAmount,
    decimal MonthCommission,
    int TodayRoundCount,
    int MonthRoundCount);

public record MonthlyReportPointDto(
    DateTime Day,
    int OrderCount,
    decimal Revenue,
    int Rounds);

public record MonthlyReportDto(
    int Year,
    int Month,
    long StoreId,
    int OrderCount,
    decimal Revenue,
    decimal RechargeAmount,
    int RoundsCount,
    decimal AverageOrder,
    IReadOnlyList<MonthlyReportPointDto> Daily);

public record YearlyReportDto(
    int Year,
    long StoreId,
    int OrderCount,
    decimal Revenue,
    int RoundsCount,
    IReadOnlyList<MonthlyReportPointDto> Monthly);

public record ServicePopularityDto(
    long ServiceId,
    string ServiceName,
    int OrderItemCount,
    int RoundsCount,
    decimal Revenue);

public record CustomerFlowPointDto(DateTime Date, int OrderCount, int UniqueMembers);
