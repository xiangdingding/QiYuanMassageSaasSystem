namespace MassageSaas.Shared.DayCloses;

public record DayClosePreviewDto(
    DateTime BusinessDate,
    long StoreId,
    int OrderCount,
    decimal RevenueTotal,
    decimal ExpectedCash,
    decimal CashAmount,
    decimal MemberCardAmount,
    decimal WechatAmount,
    decimal AlipayAmount,
    decimal BankCardAmount,
    decimal RechargeAmount,
    bool AlreadyClosed,
    int DayCloseCutoffMinutes);

public record SubmitDayCloseRequest(
    long StoreId,
    DateTime BusinessDate,
    decimal ActualCash,
    string? Remark);

public record RevokeDayCloseRequest(string? Reason);

public record DayCloseDto(
    long Id,
    long StoreId,
    DateTime BusinessDate,
    int OrderCount,
    decimal RevenueTotal,
    decimal ExpectedCash,
    decimal ActualCash,
    decimal Variance,
    decimal CashAmount,
    decimal MemberCardAmount,
    decimal WechatAmount,
    decimal AlipayAmount,
    decimal BankCardAmount,
    decimal RechargeAmount,
    long? OperatorUserId,
    string? OperatorName,
    string? Remark,
    DateTime CreatedAt);
