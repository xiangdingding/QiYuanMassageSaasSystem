namespace MassageSaas.Shared.Members;

public record MemberDto(
    long Id,
    long StoreId,
    string CardNo,
    string Phone,
    string? Name,
    string? Gender,
    DateTime? Birthday,
    decimal Balance,
    decimal TotalRecharge,
    decimal TotalConsumed,
    decimal Discount,
    string? Remark,
    DateTime CreatedAt);

public record CreateMemberRequest(
    long StoreId,
    string CardNo,
    string Phone,
    string? Name,
    string? Gender,
    DateTime? Birthday,
    decimal Discount = 1.0m,
    decimal InitialBalance = 0m,
    string? Remark = null);

public record UpdateMemberRequest(
    string Phone,
    string? Name,
    string? Gender,
    DateTime? Birthday,
    decimal Discount,
    string? Remark);

public record RechargeRequest(
    long MemberId,
    decimal Amount,
    decimal BonusAmount,
    string PayMethod,
    string? Remark);

public record RechargeRecordDto(
    long Id,
    long MemberId,
    decimal Amount,
    decimal BonusAmount,
    decimal BalanceAfter,
    string PayMethod,
    string? OperatorName,
    string? Remark,
    DateTime CreatedAt);
