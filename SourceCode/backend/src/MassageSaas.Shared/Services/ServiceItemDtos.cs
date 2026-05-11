namespace MassageSaas.Shared.Services;

public record ServiceItemDto(
    long Id,
    string Code,
    string Name,
    int DurationMinutes,
    decimal Price,
    decimal MemberPrice,
    decimal? PriceJunior,
    decimal? PriceMaster,
    string? Description,
    bool IsActive);

public record CreateServiceItemRequest(
    string Code,
    string Name,
    int DurationMinutes,
    decimal Price,
    decimal MemberPrice,
    string? Description,
    bool IsActive = true,
    decimal? PriceJunior = null,
    decimal? PriceMaster = null);

public record UpdateServiceItemRequest(
    string Name,
    int DurationMinutes,
    decimal Price,
    decimal MemberPrice,
    string? Description,
    bool IsActive,
    decimal? PriceJunior = null,
    decimal? PriceMaster = null);
