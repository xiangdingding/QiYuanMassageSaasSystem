namespace MassageSaas.Domain.Common;

/// <summary>请假折算天数计算（支持半天）。上午=整天边界，下午起首日扣 0.5，上午止末日扣 0.5。</summary>
public static class LeaveDayMath
{
    /// <summary>整段请假的折算天数（支持 0.5）。</summary>
    public static decimal Compute(DateOnly from, DateOnly to, DayHalf startHalf, DayHalf endHalf)
        => ComputeInWindow(from, to, startHalf, endHalf, from, to);

    /// <summary>落在 [windowFrom, windowTo] 内的折算请假天数（按月汇总薪资用）。</summary>
    public static decimal ComputeInWindow(
        DateOnly from, DateOnly to, DayHalf startHalf, DayHalf endHalf,
        DateOnly windowFrom, DateOnly windowTo)
    {
        var f = from > windowFrom ? from : windowFrom;
        var t = to < windowTo ? to : windowTo;
        if (t < f) return 0m;
        decimal days = t.DayNumber - f.DayNumber + 1;
        // 半天扣减只在对应边界仍落在窗口内时生效
        if (startHalf == DayHalf.Afternoon && f == from) days -= 0.5m;
        if (endHalf == DayHalf.Morning && t == to) days -= 0.5m;
        return days < 0m ? 0m : days;
    }
}
