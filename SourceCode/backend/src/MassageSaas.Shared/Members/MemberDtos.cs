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
    string Level,
    string? PreferenceNotes,
    string? HealthNotes,
    bool IsActive,
    DateTime? ClosedAt,
    string? CloseReason,
    long? ReferredByMemberId,
    string? ReferredByMemberName,
    decimal ReferralRewardEarned,
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
    string? Remark = null,
    long? ReferredByMemberId = null);

public record UpdateMemberRequest(
    string Phone,
    string? Name,
    string? Gender,
    DateTime? Birthday,
    decimal Discount,
    string? Remark,
    string? Level = null,
    string? PreferenceNotes = null,
    string? HealthNotes = null,
    long? ReferredByMemberId = null);

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
    string Kind,
    long? CounterpartyMemberId,
    string? CounterpartyMemberName,
    string? OperatorName,
    string? Remark,
    DateTime CreatedAt);

/// <summary>退卡：把会员剩余余额按指定方式退给客户，并禁用会员。</summary>
public record RefundMemberRequest(
    decimal RefundAmount,
    string RefundMethod,
    string? Reason);

/// <summary>转赠：把当前会员的全部余额转到目标会员，原会员关闭。
/// 二选一：填 ToMemberId 转给已存在会员；或填 NewMember* 字段一并新建一个目标会员。</summary>
public record TransferMemberRequest(
    long? ToMemberId,
    string? NewMemberCardNo,
    string? NewMemberPhone,
    string? NewMemberName,
    string? Reason);

/// <summary>引荐人查询：返回所有由我引荐的会员清单 + 累计返佣。</summary>
public record ReferralSummaryDto(
    long ReferrerMemberId,
    string ReferrerName,
    decimal TotalRewardEarned,
    int ReferredCount,
    IReadOnlyList<ReferredMemberDto> ReferredMembers);

public record ReferredMemberDto(
    long MemberId,
    string CardNo,
    string? Name,
    string Phone,
    decimal TotalRecharge,
    DateTime CreatedAt);
