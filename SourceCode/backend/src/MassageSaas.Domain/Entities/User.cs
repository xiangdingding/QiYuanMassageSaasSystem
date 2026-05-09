using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

public class User : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public long? StoreId { get; set; }
    public Store? Store { get; set; }

    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? RealName { get; set; }
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    public int? EmployeeNo { get; set; }
    public bool IsBlind { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
