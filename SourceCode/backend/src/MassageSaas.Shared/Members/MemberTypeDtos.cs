namespace MassageSaas.Shared.Members;

public record MemberTypeDto(
    long Id,
    string Code,
    string Name,
    int Sort,
    string Kind,
    long? ServiceItemId,
    string? ServiceItemName,
    decimal? MinRechargeAmount,
    int? MinPurchaseCount,
    decimal Discount,
    decimal? BonusAmount,
    int? BonusCount,
    int? ValidDays,
    bool IsActive,
    string? Remark,
    DateTime CreatedAt);

public record CreateMemberTypeRequest(
    string Code,
    string Name,
    int Sort,
    string Kind,
    long? ServiceItemId,
    decimal? MinRechargeAmount,
    int? MinPurchaseCount,
    decimal Discount,
    decimal? BonusAmount,
    int? BonusCount,
    int? ValidDays,
    bool IsActive = true,
    string? Remark = null);

public record UpdateMemberTypeRequest(
    string Code,
    string Name,
    int Sort,
    long? ServiceItemId,
    decimal? MinRechargeAmount,
    int? MinPurchaseCount,
    decimal Discount,
    decimal? BonusAmount,
    int? BonusCount,
    int? ValidDays,
    bool IsActive,
    string? Remark);

/// <summary>给会员办一张某类型的卡。Amount 与 PayMethod 必填；Count 仅计次卡使用。</summary>
public record IssueCardRequest(
    long MemberTypeId,
    /// <summary>充值卡：实际充值金额（≥ MinRechargeAmount）；计次卡留 0/忽略。</summary>
    decimal Amount,
    /// <summary>计次卡：购买次数（≥ MinPurchaseCount）；充值卡留 0/忽略。</summary>
    int Count,
    string PayMethod,
    string? Remark);

/// <summary>办卡后返回新卡的详情（充值卡返回更新后的会员余额；计次卡返回新建的 MemberPackage 信息）。</summary>
public record IssueCardResultDto(
    long MemberId,
    string Kind,
    decimal NewBalance,
    decimal Paid,
    decimal BonusAmount,
    int BonusCount,
    long? MemberPackageId,
    DateTime? ExpiresAt);
