using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

public class Store : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public long? ParentStoreId { get; set; }
    public Store? ParentStore { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsHeadquarters => ParentStoreId == null;

    public ICollection<Store> Branches { get; set; } = new List<Store>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
