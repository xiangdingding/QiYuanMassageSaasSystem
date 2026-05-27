using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

public class Order : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public long StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public string OrderNo { get; set; } = null!;
    public long? MemberId { get; set; }
    public Member? Member { get; set; }

    public decimal Total { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public PayMethod PayMethod { get; set; } = PayMethod.Unpaid;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public long? CashierUserId { get; set; }
    public User? CashierUser { get; set; }

    public string? Remark { get; set; }
    public string? OfflineCacheKey { get; set; }

    /// <summary>小费：客人额外给技师的钱，不计入营业额，结账时按 OrderItem 拆分到对应技师。</summary>
    public decimal TipAmount { get; set; }
    /// <summary>反结账记录：误结账后撤销，订单回到 InProgress。</summary>
    public DateTime? ReopenedAt { get; set; }
    public string? ReopenReason { get; set; }
    public long? ReopenedByUserId { get; set; }

    /// <summary>核销的优惠券/团购券（一单最多一张，简化）。</summary>
    public long? VoucherId { get; set; }
    public Voucher? Voucher { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    /// <summary>挂在该订单上的计时房费 session（同次结算的房费一起作为订单一部分展示）。</summary>
    public ICollection<TimedRoomSession> RoomSessions { get; set; } = new List<TimedRoomSession>();
}

public class OrderItem : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public long ServiceId { get; set; }
    public ServiceItem Service { get; set; } = null!;

    public long TechnicianId { get; set; }
    public User Technician { get; set; } = null!;

    public string ServiceName { get; set; } = null!;
    public int DurationMinutes { get; set; }
    /// <summary>实际收银单价：次卡核销时为 0；现金/会员卡支付时等于面值。</summary>
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    /// <summary>实际收银金额：次卡核销时为 0；其它情况等于 UnitPrice × Quantity。</summary>
    public decimal ItemTotal { get; set; }
    /// <summary>下单时记录的服务面值（无论是否走次卡都保存原价），用于小票展示"消费金额/次数"。</summary>
    public decimal ListUnitPrice { get; set; }
    public decimal CommissionAmount { get; set; }

    /// <summary>分配的房间。<see cref="RoomNoSnapshot"/> 是冗余便捷字段。</summary>
    public long? RoomId { get; set; }
    public Room? Room { get; set; }
    /// <summary>下单时房间号文本快照（即使房间改名也保留）。</summary>
    public string? RoomNoSnapshot { get; set; }

    /// <summary>转钟历史：null 表示从未转过；非空为最后一次原技师 Id。</summary>
    public long? PreviousTechnicianId { get; set; }
    public DateTime? TransferredAt { get; set; }
    public string? TransferReason { get; set; }

    /// <summary>分给该技师的小费（同一单可有多个 item，小费按 item 平摊或单独指定）。</summary>
    public decimal TipAmount { get; set; }

    /// <summary>核销了哪张计次卡/期限卡（不为 null 则该 item 走卡内次数，UnitPrice/ItemTotal=0）。</summary>
    public long? MemberPackageId { get; set; }
    public MemberPackage? MemberPackage { get; set; }

    /// <summary>是否为加钟（同一 OrderId 续做）。第一项 false，后续追加项 true。</summary>
    public bool IsAddOn { get; set; }

    /// <summary>转钟是否由投诉触发：用于按"投诉率"做技师质量报表。</summary>
    public bool ComplaintTransferred { get; set; }

    /// <summary>并钟分组键：同一技师同时服务多位客人时，相关 OrderItem 共享一个 Guid。
    /// null = 未并钟。提成仍按各 item 各自计算，此字段仅做标记与展示。</summary>
    public string? MergedGroupKey { get; set; }
}
