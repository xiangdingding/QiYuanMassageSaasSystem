using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

public class ServiceItem : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public decimal MemberPrice { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CommissionRule> CommissionRules { get; set; } = new List<CommissionRule>();
}
