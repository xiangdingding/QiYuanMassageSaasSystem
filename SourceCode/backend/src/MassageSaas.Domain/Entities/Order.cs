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

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
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
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal ItemTotal { get; set; }
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
}
