using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 会员卡套餐：计次卡（买 N 次某服务）或期限卡（月/季/年，期内不限或限次）。
/// 与 Member.Balance 的储值卡平行存在，结账时优先扣这里的次数/期限。
/// </summary>
public class MemberPackage : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public long StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public MemberPackageKind Kind { get; set; }

    /// <summary>计次卡指向的服务（期限卡可为空，表示不限项目）。</summary>
    public long? ServiceId { get; set; }
    public ServiceItem? Service { get; set; }

    public string Title { get; set; } = null!;
    public decimal PaidAmount { get; set; }

    /// <summary>计次：购买总次数；期限：限定次数（0 = 期内不限）。</summary>
    public int TotalCount { get; set; }
    public int RemainCount { get; set; }

    /// <summary>期限卡有效起止；计次卡只用 ExpiresAt（可空 = 永不过期）。</summary>
    public DateTime? ValidFrom { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public MemberPackageStatus Status { get; set; } = MemberPackageStatus.Active;
    public string? Remark { get; set; }
}
