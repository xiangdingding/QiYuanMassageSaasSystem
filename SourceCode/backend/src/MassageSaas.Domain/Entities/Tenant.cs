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

    /// <summary>顾客推荐返佣模式：None=关闭 / PercentPerRecharge=充值返佣百分比 / FixedPerCard=固定推荐费每张。百分比与固定二选一，不可并行。</summary>
    public CustomerReferralMode CustomerReferralMode { get; set; } = CustomerReferralMode.None;

    /// <summary>顾客引荐返佣百分比（0-100）。仅当 CustomerReferralMode=PercentPerRecharge 时生效。被引荐人每次充值时按 Amount × Percent / 100 给引荐顾客加余额。</summary>
    public decimal ReferralRewardPercent { get; set; }

    /// <summary>顾客引荐固定推荐费/张（≥0）。仅当 CustomerReferralMode=FixedPerCard 时生效。开卡时一次性给引荐顾客加余额。</summary>
    public decimal CustomerReferralFixedReward { get; set; }

    /// <summary>员工推荐提成模式：None=关闭 / FixedPerCard=固定金额每张 / PercentOfOpenCard=开卡实收百分比。固定与百分比二选一。</summary>
    public StaffReferralMode StaffReferralMode { get; set; } = StaffReferralMode.None;

    /// <summary>员工推荐提成·固定金额/张（≥0）。仅当 StaffReferralMode=FixedPerCard 时生效。开卡时一次性记入该员工工资。</summary>
    public decimal StaffReferralFixedAmount { get; set; }

    /// <summary>员工推荐提成·开卡实收百分比（0-100）。仅当 StaffReferralMode=PercentOfOpenCard 时生效。开卡时一次性记入该员工工资。</summary>
    public decimal StaffReferralPercent { get; set; }

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
