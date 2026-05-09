using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

public class Room : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public long StoreId { get; set; }
    public Store Store { get; set; } = null!;

    /// <summary>显示号（如 "01"、"VIP-1"）。门店内唯一。</summary>
    public string RoomNo { get; set; } = null!;
    /// <summary>容纳人数（双人间填 2，包房可填多）。</summary>
    public int Capacity { get; set; } = 1;
    /// <summary>类型：standard / vip / couple，文字标签即可。</summary>
    public string? RoomType { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; } = true;
}
