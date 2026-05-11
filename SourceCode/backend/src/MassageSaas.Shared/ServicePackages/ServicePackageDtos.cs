namespace MassageSaas.Shared.ServicePackages;

public record ServicePackageItemDto(long ServiceId, string ServiceName, int Quantity);

public record ServicePackageDto(
    long Id,
    string Code,
    string Name,
    decimal Price,
    decimal? MemberPrice,
    string? Description,
    bool IsActive,
    IReadOnlyList<ServicePackageItemDto> Items);

public record CreateServicePackageRequest(
    string Code,
    string Name,
    decimal Price,
    decimal? MemberPrice,
    string? Description,
    IReadOnlyList<ServicePackageItemInputDto> Items);

public record UpdateServicePackageRequest(
    string Name,
    decimal Price,
    decimal? MemberPrice,
    string? Description,
    bool IsActive,
    IReadOnlyList<ServicePackageItemInputDto> Items);

public record ServicePackageItemInputDto(long ServiceId, int Quantity);
