using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MassageSaas.Infrastructure.Notifications;

/// <summary>
/// 扫描所有租户的：
///   - 今日生日的会员
///   - 期限会员卡 7 日内到期
///   - 30 分钟内将到达的预约
/// 全部以幂等 DedupKey 写入 NotificationOutbox。
///
/// 跨租户扫描，所以本扫描器主动 Bypass 租户过滤。
/// </summary>
public class NotificationScanner : INotificationScanner
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IClock _clock;
    private readonly ILogger<NotificationScanner> _logger;

    private const int PackageExpiryWindowDays = 7;
    private const int AppointmentReminderMinutes = 30;

    public NotificationScanner(
        ApplicationDbContext db,
        ITenantContext tenantContext,
        IClock clock,
        ILogger<NotificationScanner> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _clock = clock;
        _logger = logger;
    }

    public async Task<int> ScanAndEnqueueAsync(CancellationToken ct)
    {
        // 扫描期间需要看所有租户的数据，所以主动 bypass
        _tenantContext.BypassTenantFilter();

        var now = _clock.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var existingKeys = new HashSet<string>(
            await _db.NotificationOutbox.AsNoTracking()
                .Where(n => n.CreatedAt > now.AddDays(-PackageExpiryWindowDays - 1))
                .Select(n => n.DedupKey)
                .ToListAsync(ct));

        var added = 0;
        added += await EnqueueBirthdaysAsync(today, existingKeys, ct);
        added += await EnqueueExpiringPackagesAsync(today, existingKeys, ct);
        added += await EnqueueUpcomingAppointmentsAsync(now, existingKeys, ct);

        if (added > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Notification scanner enqueued {Count} new notifications", added);
        }
        return added;
    }

    private async Task<int> EnqueueBirthdaysAsync(DateOnly today, HashSet<string> existingKeys, CancellationToken ct)
    {
        var members = await _db.Members.AsNoTracking()
            .Where(m => m.IsActive && m.Birthday != null
                        && m.Birthday!.Value.Month == today.Month
                        && m.Birthday!.Value.Day == today.Day)
            .Select(m => new { m.Id, m.TenantId, m.Name, m.CardNo, m.Phone })
            .ToListAsync(ct);

        var n = 0;
        foreach (var m in members)
        {
            var key = $"Birthday:{m.Id}:{today:yyyy-MM-dd}";
            if (!existingKeys.Add(key)) continue;
            var name = string.IsNullOrWhiteSpace(m.Name) ? $"会员 {m.CardNo}" : m.Name!;
            _db.NotificationOutbox.Add(new NotificationOutbox
            {
                TenantId = m.TenantId,
                Kind = NotificationKind.MemberBirthday,
                Status = NotificationStatus.Pending,
                DedupKey = key,
                MemberId = m.Id,
                RecipientPhone = m.Phone,
                Title = "生日祝福",
                Body = $"祝 {name} 生日快乐！今日到店赠送精美礼品。",
                ScheduledAt = _clock.UtcNow
            });
            n++;
        }
        return n;
    }

    private async Task<int> EnqueueExpiringPackagesAsync(DateOnly today, HashSet<string> existingKeys, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var horizon = now.AddDays(PackageExpiryWindowDays);
        var pkgs = await _db.MemberPackages.AsNoTracking()
            .Include(p => p.Member)
            .Where(p => p.Status == MemberPackageStatus.Active
                        && p.Kind == MemberPackageKind.Period
                        && p.ExpiresAt != null
                        && p.ExpiresAt!.Value > now
                        && p.ExpiresAt!.Value <= horizon)
            .ToListAsync(ct);

        var n = 0;
        foreach (var p in pkgs)
        {
            if (p.Member is null || !p.Member.IsActive) continue;
            var expireDay = DateOnly.FromDateTime(p.ExpiresAt!.Value);
            var key = $"Pkg:{p.Id}:{expireDay:yyyy-MM-dd}";
            if (!existingKeys.Add(key)) continue;
            var name = string.IsNullOrWhiteSpace(p.Member.Name) ? p.Member.CardNo : p.Member.Name!;
            var daysLeft = expireDay.DayNumber - today.DayNumber;
            _db.NotificationOutbox.Add(new NotificationOutbox
            {
                TenantId = p.TenantId,
                Kind = NotificationKind.MemberPackageExpiring,
                Status = NotificationStatus.Pending,
                DedupKey = key,
                MemberId = p.MemberId,
                RecipientPhone = p.Member.Phone,
                Title = "会员套餐即将到期",
                Body = $"{name} 的「{p.Title}」将于 {expireDay:yyyy-MM-dd} 到期（剩 {Math.Max(daysLeft, 0)} 天），可提前续卡。",
                RelatedEntityId = p.Id,
                ScheduledAt = now
            });
            n++;
        }
        return n;
    }

    private async Task<int> EnqueueUpcomingAppointmentsAsync(DateTime now, HashSet<string> existingKeys, CancellationToken ct)
    {
        var horizon = now.AddMinutes(AppointmentReminderMinutes);
        var apts = await _db.Appointments.AsNoTracking()
            .Where(a => (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
                        && a.ExpectedArriveAt > now
                        && a.ExpectedArriveAt <= horizon)
            .Select(a => new
            {
                a.Id, a.TenantId,
                a.CustomerName, a.CustomerPhone, a.CustomerOpenId,
                a.ExpectedArriveAt
            })
            .ToListAsync(ct);

        var n = 0;
        foreach (var a in apts)
        {
            var key = $"Apt:{a.Id}";
            if (!existingKeys.Add(key)) continue;
            var localTime = a.ExpectedArriveAt.ToLocalTime();
            _db.NotificationOutbox.Add(new NotificationOutbox
            {
                TenantId = a.TenantId,
                Kind = NotificationKind.AppointmentReminder,
                Status = NotificationStatus.Pending,
                DedupKey = key,
                RecipientPhone = a.CustomerPhone,
                RecipientOpenId = a.CustomerOpenId,
                Title = "预约提醒",
                Body = $"{a.CustomerName} 您好，您预约的服务约 {localTime:HH:mm} 到店，请按时前往。",
                RelatedEntityId = a.Id,
                ScheduledAt = now
            });
            n++;
        }
        return n;
    }
}
