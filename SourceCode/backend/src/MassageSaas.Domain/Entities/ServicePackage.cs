using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 服务套餐：多个服务项目一口价（如"足疗 60 + 头疗 30 = ¥288"）。
/// 套餐价独立存储，下单时把对应服务展开为 OrderItem 但 ItemTotal 按套餐价分摊。
/// </summary>
public class ServicePackage : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public decimal? MemberPrice { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ServicePackageItem> Items { get; set; } = new List<ServicePackageItem>();
}

public class ServicePackageItem : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long PackageId { get; set; }
    public ServicePackage Package { get; set; } = null!;
    public long ServiceId { get; set; }
    public ServiceItem Service { get; set; } = null!;
    public int Quantity { get; set; } = 1;
}
