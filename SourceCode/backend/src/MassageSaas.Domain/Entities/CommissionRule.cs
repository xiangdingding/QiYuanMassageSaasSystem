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

    /// <summary>适用来源：null 通配两种来源（默认，保持旧规则行为不变）；
    /// Rotation 仅适用于轮钟下的 OrderItem；Designation 仅适用于点钟下的 OrderItem。
    /// 历史数据全部为 null；新建规则可在前台 CommissionsView 里指定。</summary>
    public AssignmentSource? AssignmentSource { get; set; }
}
