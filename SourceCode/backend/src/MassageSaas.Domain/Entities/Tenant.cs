using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = null!;
    public string ContactPhone { get; set; } = null!;
    public string? ContactName { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public DateTime? ExpireAt { get; set; }
    public long? CurrentPlanId { get; set; }
    public Plan? CurrentPlan { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Store> Stores { get; set; } = new List<Store>();
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class Plan : BaseEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int MaxStores { get; set; }
    public int MaxStaff { get; set; }
    public decimal AnnualPrice { get; set; }
    public string? FeatureJson { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Subscription : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public long PlanId { get; set; }
    public Plan Plan { get; set; } = null!;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public SubscriptionSource Source { get; set; }
    public long? PaymentOrderId { get; set; }
    public PaymentOrder? PaymentOrder { get; set; }
    public string? Remark { get; set; }
}

public class PaymentOrder : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string OrderNo { get; set; } = null!;
    public decimal Amount { get; set; }
    public PaymentChannel Channel { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? ThirdPartyTransactionNo { get; set; }
    public DateTime? PaidAt { get; set; }
    public long PlanId { get; set; }
    public Plan Plan { get; set; } = null!;
    public int Years { get; set; } = 1;
    public string? RawCallbackPayload { get; set; }
}
