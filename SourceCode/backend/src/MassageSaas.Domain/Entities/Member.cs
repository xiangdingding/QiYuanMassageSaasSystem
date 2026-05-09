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
}
