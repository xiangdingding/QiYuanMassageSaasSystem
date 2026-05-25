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
    bool IsActive,
    int Sort = 0);

public record CreateServiceItemRequest(
    string Code,
    string Name,
    int DurationMinutes,
    decimal Price,
    decimal MemberPrice,
    string? Description,
    bool IsActive = true,
    decimal? PriceJunior = null,
    decimal? PriceMaster = null,
    int Sort = 0);

public record UpdateServiceItemRequest(
    string Name,
    int DurationMinutes,
    decimal Price,
    decimal MemberPrice,
    string? Description,
    bool IsActive,
    decimal? PriceJunior = null,
    decimal? PriceMaster = null,
    /// <summary>修改编码（可选）：不传或为空则保留原值；传则需在租户内唯一。</summary>
    string? Code = null,
    int Sort = 0);
