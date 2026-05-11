namespace MassageSaas.Shared.Inventory;

public record InventoryItemDto(
    long Id,
    long StoreId,
    string Code,
    string Name,
    string? Unit,
    decimal Quantity,
    decimal MinQuantity,
    decimal? UnitCost,
    string? Remark,
    bool IsActive,
    bool LowStock);

public record CreateInventoryItemRequest(
    long StoreId,
    string Code,
    string Name,
    string? Unit,
    decimal Quantity,
    decimal MinQuantity,
    decimal? UnitCost,
    string? Remark);

public record UpdateInventoryItemRequest(
    string Name,
    string? Unit,
    decimal MinQuantity,
    decimal? UnitCost,
    string? Remark,
    bool IsActive);

public record InventoryMovementDto(
    long Id,
    long ItemId,
    string ItemName,
    string Kind,
    decimal Delta,
    decimal QuantityAfter,
    long? OperatorUserId,
    string? OperatorName,
    string? Remark,
    DateTime CreatedAt);

public record CreateMovementRequest(
    long ItemId,
    string Kind,
    decimal Delta,
    string? Remark);
