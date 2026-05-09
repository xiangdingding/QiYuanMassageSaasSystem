namespace MassageSaas.Shared.Plans;

public record PlanDto(
    long Id,
    string Code,
    string Name,
    int MaxStores,
    int MaxStaff,
    decimal AnnualPrice,
    string? FeatureJson,
    bool IsActive);

public record CreatePlanRequest(
    string Code,
    string Name,
    int MaxStores,
    int MaxStaff,
    decimal AnnualPrice,
    string? FeatureJson,
    bool IsActive = true);

public record UpdatePlanRequest(
    string Name,
    int MaxStores,
    int MaxStaff,
    decimal AnnualPrice,
    string? FeatureJson,
    bool IsActive);
