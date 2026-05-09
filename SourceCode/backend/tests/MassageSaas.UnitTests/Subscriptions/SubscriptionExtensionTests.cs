using FluentAssertions;
using MassageSaas.Application.Subscriptions;

namespace MassageSaas.UnitTests.Subscriptions;

public class SubscriptionExtensionTests
{
    [Fact]
    public void Renew_WhenNotExpired_AddsYearsOnTopOfCurrentExpiry()
    {
        var now = new DateTime(2026, 5, 8, 0, 0, 0, DateTimeKind.Utc);
        var current = now.AddDays(60);

        var (start, end) = SubscriptionExtension.Renew(current, 1, now);

        start.Should().Be(now);
        end.Should().Be(current.AddYears(1));
    }

    [Fact]
    public void Renew_WhenExpired_StartsFromNow()
    {
        var now = new DateTime(2026, 5, 8, 0, 0, 0, DateTimeKind.Utc);
        var current = now.AddDays(-10);

        var (start, end) = SubscriptionExtension.Renew(current, 2, now);

        start.Should().Be(now);
        end.Should().Be(now.AddYears(2));
    }

    [Fact]
    public void Renew_WhenNoExpiryYet_StartsFromNow()
    {
        var now = new DateTime(2026, 5, 8, 0, 0, 0, DateTimeKind.Utc);

        var (start, end) = SubscriptionExtension.Renew(null, 1, now);

        start.Should().Be(now);
        end.Should().Be(now.AddYears(1));
    }

    [Fact]
    public void Renew_TreatsZeroOrNegativeYearsAsOne()
    {
        var now = new DateTime(2026, 5, 8, 0, 0, 0, DateTimeKind.Utc);

        var (_, end) = SubscriptionExtension.Renew(null, 0, now);

        end.Should().Be(now.AddYears(1));
    }
}
