using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 会员类型（卡模板）。租户自治：店主自己定义可售卡的规格。
/// 两种 Kind：
/// - <see cref="MemberTypeKind.StoredValue"/> 充值卡：开卡/续费时充钱到 Member.Balance；最低充值金额、可附加赠送金额与折扣
/// - <see cref="MemberTypeKind.CountBased"/> 计次卡：开卡/续费时绑定一个服务，购买若干次；最低次数、赠送次数、折扣
/// </summary>
public class MemberType : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>租户内唯一业务编码（大写字母+下划线）。</summary>
    public string Code { get; set; } = null!;

    /// <summary>展示名（"金卡 / 100 次足疗卡"）。</summary>
    public string Name { get; set; } = null!;

    public int Sort { get; set; }

    public MemberTypeKind Kind { get; set; }

    /// <summary>计次卡：绑定的服务项目；充值卡留空。</summary>
    public long? ServiceItemId { get; set; }
    public ServiceItem? ServiceItem { get; set; }

    /// <summary>充值卡专用：开卡/续费最低充值金额。</summary>
    public decimal? MinRechargeAmount { get; set; }

    /// <summary>计次卡专用：开卡/续费最低购买次数。</summary>
    public int? MinPurchaseCount { get; set; }

    /// <summary>折扣系数 (0,1]。开卡时复制到 Member.Discount，店主仍可单独覆盖。</summary>
    public decimal Discount { get; set; } = 1.0m;

    /// <summary>充值卡赠送金额（开卡/续费时一次性）。</summary>
    public decimal? BonusAmount { get; set; }

    /// <summary>计次卡赠送次数（开卡/续费时一次性）。</summary>
    public int? BonusCount { get; set; }

    /// <summary>有效天数：开卡日起 N 天后到期；null = 永不过期。</summary>
    public int? ValidDays { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Remark { get; set; }
}
