namespace MassageSaas.Shared.Tenants;

/// <summary>平台方查看的单租户运营概览：规模、近期营业额与技师活跃度。</summary>
public record TenantOverviewDto(
    long TenantId,
    string Name,
    string Status,
    DateTime? ExpireAt,
    int? DaysToExpire,
    string? CurrentPlanName,
    int StoreCount,
    int ActiveStoreCount,
    int StaffCount,
    int TechnicianCount,
    int MemberCount,
    decimal Revenue7Days,
    decimal Revenue30Days,
    int OrderCount30Days,
    IReadOnlyList<TenantTopTechnicianDto> TopTechnicians);

/// <summary>近 30 天按营收排名的技师。</summary>
public record TenantTopTechnicianDto(
    long TechnicianId,
    string Name,
    int RoundCount,
    decimal Revenue);
