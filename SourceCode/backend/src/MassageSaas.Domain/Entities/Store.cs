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

    /// <summary>
    /// 营业日切日时间（分钟，0-1439）。0=自然日；360=06:00 BJ。详见 BusinessDayCalculator。
    /// </summary>
    public int DayCloseCutoffMinutes { get; set; }

    public bool IsHeadquarters => ParentStoreId == null;

    public ICollection<Store> Branches { get; set; } = new List<Store>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
