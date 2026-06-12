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
    string? AssignmentSource = null,
    /// <summary>轮钟专用金额（FixedAmount/Percentage 适用）。null = 回退到 Amount。</summary>
    decimal? RotationAmount = null,
    /// <summary>点钟专用金额（FixedAmount/Percentage 适用）。null = 回退到 Amount。</summary>
    decimal? DesignationAmount = null);

public record CreateCommissionRuleRequest(
    long? ServiceId,
    long? TechnicianId,
    string RuleType,
    decimal Amount,
    string? TieredRulesJson,
    int Priority = 0,
    bool IsActive = true,
    /// <summary>适用来源：留空 = 通配两种来源；填 "Rotation" / "Designation" 区分</summary>
    string? AssignmentSource = null,
    decimal? RotationAmount = null,
    decimal? DesignationAmount = null);

public record UpdateCommissionRuleRequest(
    string RuleType,
    decimal Amount,
    string? TieredRulesJson,
    int Priority,
    bool IsActive,
    /// <summary>适用来源：留空 = 通配两种来源；填 "Rotation" / "Designation" 区分</summary>
    string? AssignmentSource = null,
    decimal? RotationAmount = null,
    decimal? DesignationAmount = null);

public record BulkCommissionRuleRequest(
    long[] ServiceIds,
    long[] TechnicianIds,
    string RuleType,
    decimal Amount,
    string? TieredRulesJson,
    int Priority,
    bool IsActive,
    decimal? RotationAmount,
    decimal? DesignationAmount,
    /// <summary>true 时遇到同 (ServiceId, TechnicianId, AssignmentSource=null) 的旧规则会更新；false 时跳过。</summary>
    bool OverwriteExisting);

public record BulkCommissionRuleResult(int Created, int Updated, int Skipped);

/// <summary>批量启用/禁用提成规则：把选中的 Ids 统一置为 IsActive。</summary>
public record BulkCommissionStatusRequest(long[] Ids, bool IsActive);

public record BulkCommissionStatusResult(int Updated);

/// <summary>批量删除提成规则：软删除选中的 Ids（启用中的会被跳过，需先禁用）。</summary>
public record BulkCommissionDeleteRequest(long[] Ids);

/// <summary>Deleted = 实际删除条数；SkippedActive = 因仍启用而跳过的条数。</summary>
public record BulkCommissionDeleteResult(int Deleted, int SkippedActive);
