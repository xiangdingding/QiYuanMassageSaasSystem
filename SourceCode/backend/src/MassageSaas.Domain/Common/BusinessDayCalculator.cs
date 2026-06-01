namespace MassageSaas.Domain.Common;

/// <summary>
/// 业务日（营业日）边界计算。
/// 按摩店常营业到凌晨，自然日和业务日不一致。每家门店配置 DayCloseCutoffMinutes
/// （0-1439，分钟），表示业务日相对北京时间 00:00 的偏移：
///   cutoffMinutes = 0   → 业务日 = 自然日（00:00 ~ 24:00 BJ）
///   cutoffMinutes = 360 → 业务日 = 06:00 BJ ~ 次日 06:00 BJ
/// </summary>
public static class BusinessDayCalculator
{
    private static readonly TimeZoneInfo BusinessTimeZone = TimeZoneInfo.CreateCustomTimeZone(
        id: "Asia/Shanghai",
        baseUtcOffset: TimeSpan.FromHours(8),
        displayName: "China Standard Time",
        standardDisplayName: "China Standard Time");

    public const int MinCutoff = 0;
    public const int MaxCutoff = 1439;

    /// <summary>给定 UTC 瞬时（默认 DateTime.UtcNow），返回它所属的业务日。</summary>
    public static DateOnly BusinessDateOf(DateTime utcMoment, int cutoffMinutes)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(EnsureUtc(utcMoment), BusinessTimeZone);
        return DateOnly.FromDateTime(local.AddMinutes(-cutoffMinutes));
    }

    /// <summary>当前业务日（基于 UTC 实时）。</summary>
    public static DateOnly TodayBusinessDate(int cutoffMinutes) =>
        BusinessDateOf(DateTime.UtcNow, cutoffMinutes);

    /// <summary>给定业务日，返回它对应的 [start, end) UTC 时间窗，用于按订单 CompletedAt 等过滤。</summary>
    public static (DateTime StartUtc, DateTime EndUtc) RangeOf(DateOnly businessDate, int cutoffMinutes)
    {
        var localStart = DateTime.SpecifyKind(
            businessDate.ToDateTime(TimeOnly.MinValue).AddMinutes(cutoffMinutes),
            DateTimeKind.Unspecified);
        var localEnd = localStart.AddDays(1);
        return (
            TimeZoneInfo.ConvertTimeToUtc(localStart, BusinessTimeZone),
            TimeZoneInfo.ConvertTimeToUtc(localEnd, BusinessTimeZone)
        );
    }

    /// <summary>业务月 = [year,month] 月第 1 天的 cutoff 时刻 → 次月 1 日 cutoff 时刻。返回 UTC。</summary>
    public static (DateTime StartUtc, DateTime EndUtc) MonthRangeOf(int year, int month, int cutoffMinutes)
    {
        var localStart = DateTime.SpecifyKind(
            new DateTime(year, month, 1).AddMinutes(cutoffMinutes),
            DateTimeKind.Unspecified);
        var localEnd = localStart.AddMonths(1);
        return (
            TimeZoneInfo.ConvertTimeToUtc(localStart, BusinessTimeZone),
            TimeZoneInfo.ConvertTimeToUtc(localEnd, BusinessTimeZone)
        );
    }

    /// <summary>多日窗口 [fromBizDate, toBizDate] 闭区间 → UTC [start, end)。</summary>
    public static (DateTime StartUtc, DateTime EndUtc) RangeOf(DateOnly fromBizDate, DateOnly toBizDate, int cutoffMinutes)
    {
        var (start, _) = RangeOf(fromBizDate, cutoffMinutes);
        var (_, end) = RangeOf(toBizDate, cutoffMinutes);
        return (start, end);
    }

    private static DateTime EnsureUtc(DateTime dt) => dt.Kind switch
    {
        DateTimeKind.Utc => dt,
        DateTimeKind.Local => dt.ToUniversalTime(),
        _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
    };
}
