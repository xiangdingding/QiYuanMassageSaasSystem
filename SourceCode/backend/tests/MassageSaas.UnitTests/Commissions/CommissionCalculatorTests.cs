using FluentAssertions;
using MassageSaas.Application.Commissions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;

namespace MassageSaas.UnitTests.Commissions;

public class CommissionCalculatorTests
{
    private static CommissionRule Rule(
        long? svc, long? tech, CommissionRuleType type, decimal amount,
        int priority = 0, string? tieredJson = null, bool active = true) =>
        new()
        {
            ServiceId = svc,
            TechnicianId = tech,
            RuleType = type,
            Amount = amount,
            Priority = priority,
            TieredRulesJson = tieredJson,
            IsActive = active
        };

    [Fact]
    public void FixedAmount_ReturnsRuleAmount()
    {
        var rules = new[] { Rule(svc: 1, tech: 10, CommissionRuleType.FixedAmount, 30m) };
        var result = CommissionCalculator.Compute(rules, 10, 1, itemTotal: 200m, durationMinutes: 60, monthCompletedQuantityIncludingThis: 1);
        result.Should().Be(30m);
    }

    [Fact]
    public void Percentage_TakesPercentOfItemTotal()
    {
        var rules = new[] { Rule(svc: null, tech: 10, CommissionRuleType.Percentage, 25m) };
        var result = CommissionCalculator.Compute(rules, 10, 1, itemTotal: 200m, durationMinutes: 60, monthCompletedQuantityIncludingThis: 1);
        result.Should().Be(50m);
    }

    [Fact]
    public void Timed_AmountTimesHours()
    {
        var rules = new[] { Rule(svc: null, tech: null, CommissionRuleType.Timed, 60m) };
        var result = CommissionCalculator.Compute(rules, 10, 1, itemTotal: 200m, durationMinutes: 90, monthCompletedQuantityIncludingThis: 1);
        result.Should().Be(90m);
    }

    [Fact]
    public void Tiered_PicksMatchingTier()
    {
        var json = "[{\"FromQty\":0,\"Amount\":20},{\"FromQty\":31,\"Amount\":30},{\"FromQty\":61,\"Amount\":40}]";
        var rules = new[] { Rule(svc: 1, tech: 10, CommissionRuleType.Tiered, 0m, tieredJson: json) };

        CommissionCalculator.Compute(rules, 10, 1, 200m, 60, 5).Should().Be(20m);
        CommissionCalculator.Compute(rules, 10, 1, 200m, 60, 31).Should().Be(30m);
        CommissionCalculator.Compute(rules, 10, 1, 200m, 60, 100).Should().Be(40m);
    }

    [Fact]
    public void TechSpecificRule_BeatsServiceRule()
    {
        var rules = new[]
        {
            Rule(svc: 1, tech: null, CommissionRuleType.FixedAmount, 10m, priority: 0),
            Rule(svc: null, tech: 10, CommissionRuleType.FixedAmount, 50m, priority: 0)
        };
        var result = CommissionCalculator.Compute(rules, 10, 1, 200m, 60, 1);
        result.Should().Be(50m, "tech-only rule has higher tier than service-only rule");
    }

    [Fact]
    public void ExactMatch_BeatsTechOnly()
    {
        var rules = new[]
        {
            Rule(svc: 1, tech: 10, CommissionRuleType.FixedAmount, 70m),
            Rule(svc: null, tech: 10, CommissionRuleType.FixedAmount, 50m)
        };
        var result = CommissionCalculator.Compute(rules, 10, 1, 200m, 60, 1);
        result.Should().Be(70m);
    }

    [Fact]
    public void HigherPriority_WinsWithinSameTier()
    {
        var rules = new[]
        {
            Rule(svc: null, tech: 10, CommissionRuleType.FixedAmount, 20m, priority: 1),
            Rule(svc: null, tech: 10, CommissionRuleType.FixedAmount, 50m, priority: 5)
        };
        var result = CommissionCalculator.Compute(rules, 10, 1, 200m, 60, 1);
        result.Should().Be(50m);
    }

    [Fact]
    public void InactiveRule_IsIgnored()
    {
        var rules = new[]
        {
            Rule(svc: 1, tech: 10, CommissionRuleType.FixedAmount, 70m, active: false),
            Rule(svc: null, tech: null, CommissionRuleType.FixedAmount, 5m)
        };
        var result = CommissionCalculator.Compute(rules, 10, 1, 200m, 60, 1);
        result.Should().Be(5m);
    }

    [Fact]
    public void NoMatchingRule_ReturnsZero()
    {
        var rules = Array.Empty<CommissionRule>();
        var result = CommissionCalculator.Compute(rules, 10, 1, 200m, 60, 1);
        result.Should().Be(0m);
    }
}
