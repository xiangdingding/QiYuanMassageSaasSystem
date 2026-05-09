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
    int TotalDurationMinutes);
