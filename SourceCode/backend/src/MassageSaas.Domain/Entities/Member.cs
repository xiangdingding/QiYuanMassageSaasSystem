using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

public class Member : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public long StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public string CardNo { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Name { get; set; }
    public string? Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public decimal Balance { get; set; }
    public decimal TotalRecharge { get; set; }
    public decimal TotalConsumed { get; set; }
    public decimal Discount { get; set; } = 1.0m;
    public string? Remark { get; set; }

    /// <summary>会员等级，按累计消费/充值升级。</summary>
    public MemberLevel Level { get; set; } = MemberLevel.Regular;
    /// <summary>客户偏好（轻按/重按、习惯部位、忌讳）。盲人技师需要文字读出来。</summary>
    public string? PreferenceNotes { get; set; }
    /// <summary>客户健康档案：高血压/孕妇/精油过敏/旧伤等。盲人技师无法目测，必须文字记录。</summary>
    public string? HealthNotes { get; set; }
}
