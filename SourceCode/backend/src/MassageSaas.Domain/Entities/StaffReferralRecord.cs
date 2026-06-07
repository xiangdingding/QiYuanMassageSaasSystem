using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 员工推荐提成记录：开卡时若指定了店内员工推荐人，按租户配置（固定额 / 开卡实收百分比）记一笔。
/// 工资单生成时按 StaffUserId + EarnedAt 当月汇总，计入 PayrollItem.ReferralCommissionTotal 并入净额。
/// </summary>
public class StaffReferralRecord : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long StoreId { get; set; }

    /// <summary>获得推荐提成的员工。</summary>
    public long StaffUserId { get; set; }
    public User StaffUser { get; set; } = null!;

    /// <summary>被推荐开出的会员卡。</summary>
    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;

    /// <summary>本笔推荐提成金额。</summary>
    public decimal Amount { get; set; }

    /// <summary>开卡（赚取）时间，用于工资单按业务月归属。</summary>
    public DateTime EarnedAt { get; set; }

    public string? Remark { get; set; }
}
