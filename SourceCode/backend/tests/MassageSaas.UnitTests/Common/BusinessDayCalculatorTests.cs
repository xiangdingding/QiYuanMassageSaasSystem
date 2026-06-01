using FluentAssertions;
using MassageSaas.Domain.Common;

namespace MassageSaas.UnitTests.Common;

public class BusinessDayCalculatorTests
{
    // 北京时间转 UTC：BJ - 8h
    private static DateTime Utc(int y, int mo, int d, int h, int mi) =>
        new DateTime(y, mo, d, h, mi, 0, DateTimeKind.Utc);
    private static DateTime BjToUtc(int y, int mo, int d, int h, int mi) =>
        Utc(y, mo, d, h, mi).AddHours(-8);

    [Fact]
    public void Cutoff_0_NaturalDay_BeijingMidnightSplits()
    {
        // 北京 2026-05-27 23:59 → 业务日 5-27
        BusinessDayCalculator.BusinessDateOf(BjToUtc(2026, 5, 27, 23, 59), 0)
            .Should().Be(new DateOnly(2026, 5, 27));
        // 北京 2026-05-28 00:00 → 业务日 5-28
        BusinessDayCalculator.BusinessDateOf(BjToUtc(2026, 5, 28, 0, 0), 0)
            .Should().Be(new DateOnly(2026, 5, 28));
    }

    [Fact]
    public void Cutoff_360_PreSixAmGoesToPreviousDay()
    {
        // 北京 2026-05-28 00:05（cutoff 06:00）→ 业务日仍是 5-27
        BusinessDayCalculator.BusinessDateOf(BjToUtc(2026, 5, 28, 0, 5), 360)
            .Should().Be(new DateOnly(2026, 5, 27));
        // 北京 2026-05-28 05:59 → 5-27
        BusinessDayCalculator.BusinessDateOf(BjToUtc(2026, 5, 28, 5, 59), 360)
            .Should().Be(new DateOnly(2026, 5, 27));
        // 北京 2026-05-28 06:00 → 5-28
        BusinessDayCalculator.BusinessDateOf(BjToUtc(2026, 5, 28, 6, 0), 360)
            .Should().Be(new DateOnly(2026, 5, 28));
    }

    [Fact]
    public void RangeOf_Cutoff360_ProducesCorrectUtcWindow()
    {
        var (start, end) = BusinessDayCalculator.RangeOf(new DateOnly(2026, 5, 27), 360);
        // 业务日 5-27 = 北京 5-27 06:00 ~ 5-28 06:00 = UTC 5-26 22:00 ~ 5-27 22:00
        start.Should().Be(Utc(2026, 5, 26, 22, 0));
        end.Should().Be(Utc(2026, 5, 27, 22, 0));
    }

    [Fact]
    public void RangeOf_Cutoff0_IsBeijingMidnightToMidnight()
    {
        var (start, end) = BusinessDayCalculator.RangeOf(new DateOnly(2026, 5, 27), 0);
        // 业务日 5-27 = 北京 5-27 00:00 ~ 5-28 00:00 = UTC 5-26 16:00 ~ 5-27 16:00
        start.Should().Be(Utc(2026, 5, 26, 16, 0));
        end.Should().Be(Utc(2026, 5, 27, 16, 0));
    }

    [Fact]
    public void MonthRangeOf_Cutoff360_StartsAtFirstDay06Bj()
    {
        var (start, end) = BusinessDayCalculator.MonthRangeOf(2026, 5, 360);
        // 业务月 = 北京 5-01 06:00 ~ 6-01 06:00 = UTC 4-30 22:00 ~ 5-31 22:00
        start.Should().Be(Utc(2026, 4, 30, 22, 0));
        end.Should().Be(Utc(2026, 5, 31, 22, 0));
    }

    [Fact]
    public void BusinessDateOf_Reverses_RangeOf_AtAnyMoment()
    {
        // 任意业务日的中间时刻反算回去应得同一业务日
        foreach (var cutoff in new[] { 0, 120, 360, 1380 })
        foreach (var d in new[] { 1, 15, 28 })
        {
            var biz = new DateOnly(2026, 5, d);
            var (start, end) = BusinessDayCalculator.RangeOf(biz, cutoff);
            BusinessDayCalculator.BusinessDateOf(start, cutoff).Should().Be(biz);
            // end 是下一个业务日的 start，不应再属于本日
            BusinessDayCalculator.BusinessDateOf(end, cutoff).Should().Be(biz.AddDays(1));
        }
    }
}
