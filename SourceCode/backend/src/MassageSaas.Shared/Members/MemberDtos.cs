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
    long? ReferredByStaffId,
    string? ReferredByStaffName,
    decimal ReferralRewardEarned,
    string? WechatOpenId,
    DateTime CreatedAt,
    long? MemberTypeId = null,
    string? MemberTypeName = null,
    string? MemberTypeKind = null,
    /// <summary>次卡专属：当前会员名下所有活动计次套餐的累计购买次数（含赠送）；非次卡为 null。</summary>
    int? TotalCount = null,
    /// <summary>次卡专属：当前会员名下所有活动计次套餐的剩余次数；非次卡为 null。</summary>
    int? RemainCount = null,
    /// <summary>次卡专属：会员类型模板绑定的服务项目 Id（用于结账时校验购物车里是否含该服务）；非次卡为 null。</summary>
    long? ServiceItemId = null,
    /// <summary>次卡专属：绑定服务项目名称，便于前端提示"无匹配项目"。</summary>
    string? ServiceItemName = null,
    /// <summary>会员卡到期时间 = 开卡日(CreatedAt) + 会员类型 ValidDays；null = 永久有效。</summary>
    DateTime? CardExpiresAt = null,
    /// <summary>距到期剩余天数（按北京日历日计算）；负数=已过期；null=永久。</summary>
    int? CardDaysRemaining = null);

/// <summary>按手机号聚合的会员视图：一个人名下可能有多张卡（充值卡 + 计次卡）。</summary>
public record MemberPhoneGroupDto(
    string Phone,
    string? PrimaryName,
    int CardCount,
    decimal TotalBalance,
    decimal TotalRecharge,
    decimal TotalConsumed,
    bool HasInactive,
    IReadOnlyList<MemberDto> Cards);

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
    long? ReferredByMemberId = null,
    /// <summary>店内员工推荐人（可与顾客推荐人并存）；开卡时按租户配置给该员工记一笔推荐提成。</summary>
    long? ReferredByStaffId = null,
    /// <summary>开卡时绑定的会员类型；选了即按模板规则校验最低充值/最低次数并写赠送、折扣、到期日。</summary>
    long? MemberTypeId = null,
    /// <summary>仅计次卡：购买次数（必须 ≥ 模板 MinPurchaseCount）。</summary>
    int Count = 0,
    /// <summary>开卡支付来源（Cash/Wechat/Alipay/BankCard 等）；为空回退到 Cash。</summary>
    string? PayMethod = null);

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
    long? ReferredByMemberId = null,
    string? WechatOpenId = null,
    /// <summary>店内员工推荐人。仅当 <see cref="UpdateStaffReferral"/>=true 时生效：可改派他人或置空（null=清除）。</summary>
    long? ReferredByStaffId = null,
    /// <summary>true 时才按 <see cref="ReferredByStaffId"/> 调整员工推荐（含改派/置空并对账推荐提成）；
    /// false（默认）保持原值不动，兼容不改推荐人的调用方（如旧 BS 编辑）。</summary>
    bool UpdateStaffReferral = false,
    /// <summary>true 时才按 <see cref="ReferredByMemberId"/> 调整顾客引荐人（含改人/置空，null=清除）；
    /// false（默认）保持原值不动，兼容不改引荐人的调用方。</summary>
    bool UpdateReferredByMember = false);

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

/// <summary>会员消费记录（流水「消费记录」页）：对应 GET /members/{id}/orders 的投影。</summary>
public record MemberConsumptionDto(
    long Id,
    string OrderNo,
    decimal Total,
    decimal PaidAmount,
    string PayMethod,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt);
