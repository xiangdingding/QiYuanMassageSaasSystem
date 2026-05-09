namespace MassageSaas.Application.Subscriptions;

public static class SubscriptionExtension
{
    /// <summary>
    /// 计算续费后的新到期时间。
    /// 当前到期日仍未过期 -> 在原到期日上 +N 年；
    /// 已过期 -> 从当前时间 +N 年。
    /// </summary>
    public static (DateTime startAt, DateTime endAt) Renew(DateTime? currentExpireAt, int years, DateTime now)
    {
        if (years < 1) years = 1;
        var anchor = currentExpireAt.HasValue && currentExpireAt.Value > now
            ? currentExpireAt.Value
            : now;
        return (now, anchor.AddYears(years));
    }
}
