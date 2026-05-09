using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

public class TechnicianQueue : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public long TechnicianId { get; set; }
    public User Technician { get; set; } = null!;

    public QueueState State { get; set; } = QueueState.Idle;
    public DateTime? EnteredAt { get; set; }
    public DateTime? LastCalledAt { get; set; }
    public int QueuePosition { get; set; }
    public int TodayRoundCount { get; set; }
}
