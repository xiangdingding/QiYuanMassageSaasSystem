using System.Text.Json;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;

namespace MassageSaas.Application.Commissions;

/// <summary>
/// 提成计算器：按"技师+服务+来源"维度从规则集中匹配最高优先级规则，再按规则类型计算单笔提成。
///
/// 规则匹配优先顺序（任一档命中即返回，下一档不再考虑）：
///   1) 同时匹配 ServiceId + TechnicianId
///   2) 仅匹配 TechnicianId（适用所有服务）
///   3) 仅匹配 ServiceId（适用所有技师）
///   4) 通用规则（ServiceId/TechnicianId 都为 null）
///
/// 每一档内部，先按"来源是否精确命中"再按 Priority、Id 排序：
///   - source 精确匹配（rule.AssignmentSource == itemSource）优先于通配（rule.AssignmentSource == null）
///   - 同样 source 等级下再按 Priority desc、Id desc
///   - 与 itemSource 不一致的 source-specific 规则不入选
///
/// 历史 OrderItem（itemSource = Unknown）只会匹配 source = null 的通配规则，
/// 保证旧数据计算结果与旧版完全一致（无 source-aware 规则时表现等同从前）。
/// </summary>
public static class CommissionCalculator
{
    public record TieredEntry(int FromQty, decimal Amount);

    public static decimal Compute(
        IEnumerable<CommissionRule> tenantRules,
        long technicianId,
        long serviceId,
        decimal itemTotal,
        int durationMinutes,
        int monthCompletedQuantityIncludingThis,
        // 默认 Designation 与 OrdersController.ParseSource 的兜底口径一致；
        // 生产路径（Checkout）总会显式传 item.AssignmentSource，所以默认值仅作老测试的兼容
        AssignmentSource itemSource = AssignmentSource.Designation)
    {
        var active = tenantRules.Where(r => r.IsActive).ToList();

        var rule =
            FindBest(active, itemSource, r => r.ServiceId == serviceId && r.TechnicianId == technicianId)
            ?? FindBest(active, itemSource, r => r.ServiceId == null && r.TechnicianId == technicianId)
            ?? FindBest(active, itemSource, r => r.ServiceId == serviceId && r.TechnicianId == null)
            ?? FindBest(active, itemSource, r => r.ServiceId == null && r.TechnicianId == null);

        if (rule is null) return 0m;

        // FixedAmount / Percentage 支持"一条规则双金额"：按 itemSource 选 RotationAmount/DesignationAmount；
        // 缺失则回退到 Amount。Timed / Tiered 维持原口径，不参与双金额（阶梯本身已是多档结构）。
        var pickedAmount = PickAmountBySource(rule, itemSource);

        return rule.RuleType switch
        {
            CommissionRuleType.FixedAmount => pickedAmount,
            CommissionRuleType.Percentage => Round(itemTotal * (pickedAmount / 100m)),
            CommissionRuleType.Timed => Round(rule.Amount * durationMinutes / 60m),
            CommissionRuleType.Tiered => ComputeTiered(rule, monthCompletedQuantityIncludingThis),
            _ => 0m
        };
    }

    private static decimal PickAmountBySource(CommissionRule rule, AssignmentSource itemSource) =>
        itemSource switch
        {
            AssignmentSource.Rotation => rule.RotationAmount ?? rule.Amount,
            AssignmentSource.Designation => rule.DesignationAmount ?? rule.Amount,
            _ => rule.Amount
        };

    private static CommissionRule? FindBest(
        IEnumerable<CommissionRule> rules,
        AssignmentSource itemSource,
        Func<CommissionRule, bool> tierPredicate)
        => rules
            .Where(tierPredicate)
            // 只接受通配规则，或来源精确匹配的规则；不同来源的 source-specific 规则不入选
            .Where(r => !r.AssignmentSource.HasValue || r.AssignmentSource.Value == itemSource)
            // source 精确命中优先于通配；其后按 Priority、Id
            .OrderByDescending(r => r.AssignmentSource.HasValue ? 1 : 0)
            .ThenByDescending(r => r.Priority)
            .ThenByDescending(r => r.Id)
            .FirstOrDefault();

    private static decimal ComputeTiered(CommissionRule rule, int qty)
    {
        if (string.IsNullOrWhiteSpace(rule.TieredRulesJson)) return rule.Amount;

        List<TieredEntry>? tiers;
        try
        {
            tiers = JsonSerializer.Deserialize<List<TieredEntry>>(
                rule.TieredRulesJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return rule.Amount;
        }

        if (tiers is null || tiers.Count == 0) return rule.Amount;

        var ordered = tiers.OrderBy(t => t.FromQty).ToList();
        var match = ordered.LastOrDefault(t => qty >= t.FromQty);
        return Round(match?.Amount ?? rule.Amount);
    }

    private static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}
