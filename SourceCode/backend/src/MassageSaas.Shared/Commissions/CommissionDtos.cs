namespace MassageSaas.Shared.Commissions;

public record CommissionRuleDto(
    long Id,
    long? ServiceId,
    string? ServiceName,
    long? TechnicianId,
    string? TechnicianName,
    string RuleType,
    decimal Amount,
    string? TieredRulesJson,
    int Priority,
    bool IsActive);

public record CreateCommissionRuleRequest(
    long? ServiceId,
    long? TechnicianId,
    string RuleType,
    decimal Amount,
    string? TieredRulesJson,
    int Priority = 0,
    bool IsActive = true);

public record UpdateCommissionRuleRequest(
    string RuleType,
    decimal Amount,
    string? TieredRulesJson,
    int Priority,
    bool IsActive);
