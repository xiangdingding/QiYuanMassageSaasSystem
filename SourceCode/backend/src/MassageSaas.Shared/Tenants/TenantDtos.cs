namespace MassageSaas.Shared.Tenants;

public record TenantSummaryDto(
    long Id,
    string Name,
    string ContactPhone,
    string? ContactName,
    string Status,
    DateTime? ExpireAt,
    long? CurrentPlanId,
    string? CurrentPlanName,
    int? DaysToExpire,
    DateTime? SubscriptionStartAt,
    int? SubscriptionYears);

public record TenantDetailDto(
    long Id,
    string Name,
    string ContactPhone,
    string? ContactName,
    string Status,
    DateTime? ExpireAt,
    long? CurrentPlanId,
    string? CurrentPlanName,
    int StoreCount,
    int UserCount,
    DateTime CreatedAt);

public record CreateTenantRequest(
    string Name,
    string ContactPhone,
    string? ContactName,
    string OwnerUsername,
    string OwnerPassword,
    string? OwnerRealName,
    string HeadquartersName);

public record UpdateTenantStatusRequest(string Status);
