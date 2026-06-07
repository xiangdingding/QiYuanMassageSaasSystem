namespace MassageSaas.Shared.Settings;

/// <summary>
/// 推荐规则（全店统一）。放在规则模块"推荐规则" Tab。
/// 顾客推荐：模式二选一或关闭——返佣百分比（每次充值）或 固定推荐费（开卡一次性），到推荐顾客余额。
/// 员工推荐：模式（关闭/固定每张/开卡实收百分比）+ 按模式分开的数值（固定金额、百分比各存一份），开卡时记入该员工工资。
/// </summary>
public record ReferralSettingDto(
    string CustomerReferralMode,
    decimal CustomerRewardPercent,
    decimal CustomerFixedReward,
    string StaffReferralMode,
    decimal StaffReferralFixedAmount,
    decimal StaffReferralPercent);

public record UpdateReferralSettingRequest(
    string CustomerReferralMode,
    decimal CustomerRewardPercent,
    decimal CustomerFixedReward,
    string StaffReferralMode,
    decimal StaffReferralFixedAmount,
    decimal StaffReferralPercent);
