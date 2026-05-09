using System.Text.Json;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;

namespace MassageSaas.Application.Commissions;

/// <summary>
/// 提成计算器：按"技师+服务"维度从规则集中匹配最高优先级规则，再按规则类型计算单笔提成。
///
/// 规则匹配优先顺序（同优先级取 Priority 最大）：
///   1) 同时匹配 ServiceId + TechnicianId
///   2) 仅匹配 TechnicianId（适用所有服务）
///   3) 仅匹配 ServiceId（适用所有技师）
///   4) 通用规则（ServiceId/TechnicianId 都为 null）
///
/// 任一档命中即返回，下一档不再考虑。
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
        int monthCompletedQuantityIncludingThis)
    {
        var active = tenantRules.Where(r => r.IsActive).ToList();

        var rule =
            FindBest(active, r => r.ServiceId == serviceId && r.TechnicianId == technicianId)
            ?? FindBest(active, r => r.ServiceId == null && r.TechnicianId == technicianId)
            ?? FindBest(active, r => r.ServiceId == serviceId && r.TechnicianId == null)
            ?? FindBest(active, r => r.ServiceId == null && r.TechnicianId == null);

        if (rule is null) return 0m;

        return rule.RuleType switch
        {
            CommissionRuleType.FixedAmount => rule.Amount,
            CommissionRuleType.Percentage => Round(itemTotal * (rule.Amount / 100m)),
            CommissionRuleType.Timed => Round(rule.Amount * durationMinutes / 60m),
            CommissionRuleType.Tiered => ComputeTiered(rule, monthCompletedQuantityIncludingThis),
            _ => 0m
        };
    }

    private static CommissionRule? FindBest(IEnumerable<CommissionRule> rules, Func<CommissionRule, bool> predicate)
        => rules.Where(predicate).OrderByDescending(r => r.Priority).ThenByDescending(r => r.Id).FirstOrDefault();

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
