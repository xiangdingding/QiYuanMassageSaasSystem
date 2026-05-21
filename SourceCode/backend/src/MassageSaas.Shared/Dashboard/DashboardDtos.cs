namespace MassageSaas.Shared.Dashboard;

public record PlatformDashboardDto(
    int TotalTenants,
    int ActiveTenants,
    int TrialTenants,
    int ExpiredTenants,
    int DisabledTenants,
    int ExpiringIn30Days,
    int ExpiringIn7Days,
    decimal RevenueLast30Days,
    decimal RevenueThisYear,
    int PaidOrdersLast30Days,
    IReadOnlyList<RecentSubscriptionDto> RecentSubscriptions);

public record RecentSubscriptionDto(
    long Id,
    long TenantId,
    string TenantName,
    string PlanName,
    decimal Amount,
    string Source,
    DateTime CreatedAt);
