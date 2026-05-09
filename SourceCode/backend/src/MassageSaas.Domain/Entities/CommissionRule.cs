using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

public class CommissionRule : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public long? ServiceId { get; set; }
    public ServiceItem? Service { get; set; }

    public long? TechnicianId { get; set; }
    public User? Technician { get; set; }

    public CommissionRuleType RuleType { get; set; }
    public decimal Amount { get; set; }
    public string? TieredRulesJson { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
}
