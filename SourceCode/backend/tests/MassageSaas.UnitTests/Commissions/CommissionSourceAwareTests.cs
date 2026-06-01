using FluentAssertions;
using MassageSaas.Application.Commissions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;

namespace MassageSaas.UnitTests.Commissions;

/// <summary>
/// 轮钟 / 点钟 提成规则匹配行为：CommissionRule.AssignmentSource 加入后，
/// 同一档（svc+tech / tech / svc / 全局）内 source 精确匹配规则优先于通配（source=null）规则；
/// 跨 source 的 source-specific 规则不应误命中；老规则（source=null）行为完全不变。
/// </summary>
public class CommissionSourceAwareTests
{
    private static CommissionRule Rule(
        long? svc, long? tech, AssignmentSource? source,
        CommissionRuleType type, decimal amount,
        int priority = 0, bool active = true) =>
        new()
        {
            ServiceId = svc,
            TechnicianId = tech,
            AssignmentSource = source,
            RuleType = type,
            Amount = amount,
            Priority = priority,
            IsActive = active
        };

    [Fact]
    public void Compute_PrefersSourceMatchingRuleOverWildcard()
    {
        // 同 svc+tech：null 通配 50 元固定 vs Rotation 专属 80 元固定；itemSource=Rotation 应取 Rotation 那条
        var rules = new[]
        {
            Rule(svc: 1, tech: 10, source: null, CommissionRuleType.FixedAmount, 50m),
            Rule(svc: 1, tech: 10, source: AssignmentSource.Rotation, CommissionRuleType.FixedAmount, 80m)
        };
        var result = CommissionCalculator.Compute(
            rules, technicianId: 10, serviceId: 1,
            itemTotal: 200m, durationMinutes: 60, monthCompletedQuantityIncludingThis: 1,
            itemSource: AssignmentSource.Rotation);
        result.Should().Be(80m, "Rotation 专属规则应该压过 null 通配");
    }

    [Fact]
    public void Compute_FallsBackToWildcardWhenNoSourceRule()
    {
        // 只配了 null 通配 → 任何 itemSource 都用它（保 backward-compat）
        var rules = new[]
        {
            Rule(svc: 1, tech: 10, source: null, CommissionRuleType.Percentage, 20m)
        };

        CommissionCalculator.Compute(rules, 10, 1, 200m, 60, 1, AssignmentSource.Rotation)
            .Should().Be(40m);
        CommissionCalculator.Compute(rules, 10, 1, 200m, 60, 1, AssignmentSource.Designation)
            .Should().Be(40m);
        CommissionCalculator.Compute(rules, 10, 1, 200m, 60, 1, AssignmentSource.Unknown)
            .Should().Be(40m, "历史 Unknown 行也走通配规则，结果与旧版一致");
    }

    [Fact]
    public void Compute_IgnoresRuleOfDifferentSource()
    {
        // 同 svc+tech 只有 Designation 专属规则；item 是 Rotation 不该命中，会落到下一档
        var rules = new[]
        {
            Rule(svc: 1, tech: 10, source: AssignmentSource.Designation, CommissionRuleType.FixedAmount, 100m),
            // 下一档（tech-only）的通配规则才是它该走的
            Rule(svc: null, tech: 10, source: null, CommissionRuleType.FixedAmount, 20m)
        };
        var result = CommissionCalculator.Compute(
            rules, 10, 1, 200m, 60, 1, AssignmentSource.Rotation);
        result.Should().Be(20m, "Designation 专属规则不能匹配 Rotation 项；落到 tech-only 通配");
    }

    [Fact]
    public void Compute_RotationAndDesignationCanCoexistWithDifferentAmounts()
    {
        // 同 svc+tech 两条规则：轮钟 30 元固定，点钟 20% 抽成
        var rules = new[]
        {
            Rule(svc: 1, tech: 10, source: AssignmentSource.Rotation, CommissionRuleType.FixedAmount, 30m),
            Rule(svc: 1, tech: 10, source: AssignmentSource.Designation, CommissionRuleType.Percentage, 20m)
        };

        CommissionCalculator.Compute(rules, 10, 1, 200m, 60, 1, AssignmentSource.Rotation)
            .Should().Be(30m);
        CommissionCalculator.Compute(rules, 10, 1, 200m, 60, 1, AssignmentSource.Designation)
            .Should().Be(40m, "200 × 20% = 40");
    }

    [Fact]
    public void Compute_UnknownItemSource_OnlyMatchesWildcardRules()
    {
        // 历史 OrderItem (AssignmentSource = Unknown) 不应命中任何 source-specific 规则，
        // 只能走 source=null 的通配规则，否则会被新规则错算
        var rules = new[]
        {
            Rule(svc: 1, tech: 10, source: AssignmentSource.Rotation, CommissionRuleType.FixedAmount, 999m),
            Rule(svc: 1, tech: 10, source: null, CommissionRuleType.FixedAmount, 25m)
        };
        var result = CommissionCalculator.Compute(
            rules, 10, 1, 200m, 60, 1, AssignmentSource.Unknown);
        result.Should().Be(25m, "Unknown 只能匹配通配规则");
    }

    [Fact]
    public void Compute_DualAmount_FixedAmountPicksBySource()
    {
        // 一条通配规则，但分别配置了轮钟金额 30、点钟金额 50
        var rule = Rule(svc: 1, tech: 10, source: null, CommissionRuleType.FixedAmount, 0m);
        rule.RotationAmount = 30m;
        rule.DesignationAmount = 50m;

        CommissionCalculator.Compute(new[] { rule }, 10, 1, 200m, 60, 1, AssignmentSource.Rotation)
            .Should().Be(30m, "Rotation 应使用 RotationAmount");
        CommissionCalculator.Compute(new[] { rule }, 10, 1, 200m, 60, 1, AssignmentSource.Designation)
            .Should().Be(50m, "Designation 应使用 DesignationAmount");
    }

    [Fact]
    public void Compute_DualAmount_PercentageAppliesPerSource()
    {
        // 百分比：轮钟 20%、点钟 30%
        var rule = Rule(svc: 1, tech: 10, source: null, CommissionRuleType.Percentage, 0m);
        rule.RotationAmount = 20m;
        rule.DesignationAmount = 30m;

        CommissionCalculator.Compute(new[] { rule }, 10, 1, 200m, 60, 1, AssignmentSource.Rotation)
            .Should().Be(40m, "200 × 20% = 40");
        CommissionCalculator.Compute(new[] { rule }, 10, 1, 200m, 60, 1, AssignmentSource.Designation)
            .Should().Be(60m, "200 × 30% = 60");
    }

    [Fact]
    public void Compute_DualAmount_OnlyOneSideFilled_OtherSideFallsBackToAmount()
    {
        // 只填了轮钟金额，点钟保持空 → 点钟应该回退到 Amount（兼容老规则视角）
        var rule = Rule(svc: 1, tech: 10, source: null, CommissionRuleType.FixedAmount, 25m);
        rule.RotationAmount = 80m;
        // DesignationAmount 留空

        CommissionCalculator.Compute(new[] { rule }, 10, 1, 200m, 60, 1, AssignmentSource.Rotation)
            .Should().Be(80m);
        CommissionCalculator.Compute(new[] { rule }, 10, 1, 200m, 60, 1, AssignmentSource.Designation)
            .Should().Be(25m, "DesignationAmount 为空时回退到 Amount=25");
    }

    [Fact]
    public void Compute_DualAmount_TimedRuleIgnoresDualFieldsAndUsesAmount()
    {
        // Timed 类型不参与双金额；即使设置了 RotationAmount 也应忽略，按 Amount * minutes/60 计算
        var rule = Rule(svc: 1, tech: 10, source: null, CommissionRuleType.Timed, 60m);
        rule.RotationAmount = 999m; // 不应影响

        CommissionCalculator.Compute(new[] { rule }, 10, 1, 200m, durationMinutes: 30, 1, AssignmentSource.Rotation)
            .Should().Be(30m, "60元/h × 0.5h = 30，与 RotationAmount 无关");
    }

    [Fact]
    public void Compute_SourceMatchBeatsPriorityWithinSameTier()
    {
        // 同一档：通配规则 Priority=50，Rotation 专属规则 Priority=1；itemSource=Rotation 仍应取 Rotation 那条
        // 因为 OrderBy 里 source-specific 优先于 Priority
        var rules = new[]
        {
            Rule(svc: 1, tech: 10, source: null, CommissionRuleType.FixedAmount, 99m, priority: 50),
            Rule(svc: 1, tech: 10, source: AssignmentSource.Rotation, CommissionRuleType.FixedAmount, 33m, priority: 1)
        };
        var result = CommissionCalculator.Compute(
            rules, 10, 1, 200m, 60, 1, AssignmentSource.Rotation);
        result.Should().Be(33m, "source 精确命中是匹配的第一权重，Priority 在其之后");
    }
}
