namespace MassageSaas.Shared.Services;

public record ServiceItemDto(
    long Id,
    string Code,
    string Name,
    int DurationMinutes,
    decimal Price,
    decimal MemberPrice,
    string? Description,
    bool IsActive);

public record CreateServiceItemRequest(
    string Code,
    string Name,
    int DurationMinutes,
    decimal Price,
    decimal MemberPrice,
    string? Description,
    bool IsActive = true);

public record UpdateServiceItemRequest(
    string Name,
    int DurationMinutes,
    decimal Price,
    decimal MemberPrice,
    string? Description,
    bool IsActive);
