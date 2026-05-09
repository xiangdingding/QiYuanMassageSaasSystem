namespace MassageSaas.Shared.Stores;

public record StoreDto(
    long Id,
    string Name,
    string? Address,
    string? Phone,
    bool IsActive,
    bool IsHeadquarters,
    long? ParentStoreId,
    DateTime CreatedAt);

public record CreateStoreRequest(
    string Name,
    string? Address,
    string? Phone,
    long? ParentStoreId);

public record UpdateStoreRequest(
    string Name,
    string? Address,
    string? Phone,
    bool IsActive);
