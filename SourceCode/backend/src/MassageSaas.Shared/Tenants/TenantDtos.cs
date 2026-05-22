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
    string OwnerPhone,
    string OwnerPassword,
    string? OwnerRealName,
    string HeadquartersName,
    int? TrialDays);

/// <summary>按摩店自助注册请求（公开匿名端点）。试用天数由系统默认（30），不接受外部指定。</summary>
public record RegisterTenantRequest(
    string Name,
    string ContactPhone,
    string? ContactName,
    string OwnerPhone,
    string OwnerPassword,
    string? OwnerRealName);

/// <summary>注册成功响应。OwnerPhone 即店主的登录手机号。</summary>
public record RegisterTenantResponse(
    long TenantId,
    string TenantName,
    string OwnerPhone,
    DateTime ExpireAt,
    int TrialDays);

public record UpdateTenantStatusRequest(string Status);
