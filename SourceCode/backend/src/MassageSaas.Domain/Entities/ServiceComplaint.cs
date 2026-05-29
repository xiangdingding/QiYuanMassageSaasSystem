using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 客户投诉。常规情况下挂在 OrderItem 上，由收银员/店长录入；
/// 也允许"匿名投诉"——客人记不清单号或针对整体服务体验时 OrderItemId/OrderId/OriginalTechnicianId 为空。
/// 后续由店长选择处理方式（改派/退款/道歉/不处理）。
/// 仅当 OrderItem 存在时，改派会调用转钟流程并把 OrderItem.ComplaintTransferred 置 true。
/// </summary>
public class ServiceComplaint : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public long? OrderId { get; set; }
    public Order? Order { get; set; }
    public long? OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }

    /// <summary>被投诉的技师（投诉发生时 OrderItem.TechnicianId 的快照）。匿名投诉时为空。</summary>
    public long? OriginalTechnicianId { get; set; }
    public User? OriginalTechnician { get; set; }

    public long? MemberId { get; set; }
    public Member? Member { get; set; }

    /// <summary>投诉标签（逗号分隔，如 "态度差,力度不合适,房间脏"），便于聚合统计。</summary>
    public string? Tags { get; set; }
    public string? Comment { get; set; }

    public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;

    /// <summary>处理方式（未处理时为 null）。</summary>
    public ComplaintResolution? Resolution { get; set; }
    /// <summary>改派目标技师（仅 Resolution=Reassigned 时填）。</summary>
    public long? ReassignedToTechnicianId { get; set; }
    public User? ReassignedToTechnician { get; set; }
    public string? ResolutionNote { get; set; }

    public DateTime? ResolvedAt { get; set; }
    public long? ResolvedByUserId { get; set; }
    public User? ResolvedByUser { get; set; }

    public long? RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }
}
