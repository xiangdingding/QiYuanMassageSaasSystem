using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

public class Appointment : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public long StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public long? ServiceId { get; set; }
    public ServiceItem? Service { get; set; }
    public long? PreferredTechnicianId { get; set; }
    public User? PreferredTechnician { get; set; }

    public string CustomerName { get; set; } = null!;
    public string CustomerPhone { get; set; } = null!;
    public string? CustomerOpenId { get; set; }
    public DateTime ExpectedArriveAt { get; set; }
    public int PartySize { get; set; } = 1;
    public string? Remark { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
