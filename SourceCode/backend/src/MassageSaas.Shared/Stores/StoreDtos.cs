namespace MassageSaas.Shared.Stores;

public record StoreDto(
    long Id,
    string Name,
    string? Address,
    string? Phone,
    bool IsActive,
    bool IsHeadquarters,
    long? ParentStoreId,
    int DayCloseCutoffMinutes,
    DateTime CreatedAt);

public record CreateStoreRequest(
    string Name,
    string? Address,
    string? Phone,
    long? ParentStoreId,
    int DayCloseCutoffMinutes = 0);

public record UpdateStoreRequest(
    string Name,
    string? Address,
    string? Phone,
    bool IsActive,
    int DayCloseCutoffMinutes = 0);
