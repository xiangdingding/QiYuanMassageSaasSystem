using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 顾客对一次服务的评价（关联具体 OrderItem，因此能精确到技师）。
/// 行业里店主很关心客诉率/好评率，盲人技师也希望知道哪点要改进。
/// </summary>
public class ServiceReview : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public long OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;
    public long TechnicianId { get; set; }
    public User Technician { get; set; } = null!;
    public long? MemberId { get; set; }
    public Member? Member { get; set; }

    /// <summary>1-5 星。</summary>
    public int Rating { get; set; }
    public string? Tags { get; set; }   // 逗号分隔标签："手法专业,态度好"
    public string? Comment { get; set; }
}
