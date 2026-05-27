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
    bool IsActive,
    /// <summary>适用来源："Rotation" 仅匹配轮钟 / "Designation" 仅匹配点钟 / null 通配两种</summary>
    string? AssignmentSource = null);

public record CreateCommissionRuleRequest(
    long? ServiceId,
    long? TechnicianId,
    string RuleType,
    decimal Amount,
    string? TieredRulesJson,
    int Priority = 0,
    bool IsActive = true,
    /// <summary>适用来源：留空 = 通配两种来源；填 "Rotation" / "Designation" 区分</summary>
    string? AssignmentSource = null);

public record UpdateCommissionRuleRequest(
    string RuleType,
    decimal Amount,
    string? TieredRulesJson,
    int Priority,
    bool IsActive,
    /// <summary>适用来源：留空 = 通配两种来源；填 "Rotation" / "Designation" 区分</summary>
    string? AssignmentSource = null);
