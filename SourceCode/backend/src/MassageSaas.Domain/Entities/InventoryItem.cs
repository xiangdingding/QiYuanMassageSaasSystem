using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 物耗：精油 / 毛巾 / 足浴包 / 一次性用品 等耗材。
/// 库存按门店分别记。
/// </summary>
public class InventoryItem : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long StoreId { get; set; }
    public Store Store { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Unit { get; set; }   // 瓶 / 包 / 个
    public decimal Quantity { get; set; }
    public decimal MinQuantity { get; set; }   // 低于此量库存预警
    public decimal? UnitCost { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 库存出入记录（采购入库 / 服务消耗 / 盘点调整 / 报损）。
/// </summary>
public class InventoryMovement : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long ItemId { get; set; }
    public InventoryItem Item { get; set; } = null!;
    public InventoryMovementKind Kind { get; set; }
    public decimal Delta { get; set; }   // 正数=入；负数=出
    public decimal QuantityAfter { get; set; }
    public long? OperatorUserId { get; set; }
    public User? OperatorUser { get; set; }
    public string? Remark { get; set; }
}
