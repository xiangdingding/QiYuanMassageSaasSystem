using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Common;
using MassageSaas.Shared.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Policy = "ShopStaff")]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationScanner _scanner;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IClock _clock;

    public NotificationsController(
        ApplicationDbContext db,
        INotificationScanner scanner,
        INotificationDispatcher dispatcher,
        IClock clock)
    {
        _db = db;
        _scanner = scanner;
        _dispatcher = dispatcher;
        _clock = clock;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? kind = null,
        CancellationToken ct = default)
    {
        var pq = new PageQuery(page, pageSize, null);
        var q = _db.NotificationOutbox.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<NotificationStatus>(status, true, out var st))
            q = q.Where(n => n.Status == st);
        if (!string.IsNullOrWhiteSpace(kind) && Enum.TryParse<NotificationKind>(kind, true, out var kn))
            q = q.Where(n => n.Kind == kn);

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pq.SafePage - 1) * pq.SafePageSize)
            .Take(pq.SafePageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Kind.ToString(), n.Status.ToString(), n.DedupKey,
                n.MemberId, n.RecipientPhone, n.RecipientOpenId,
                n.Title, n.Body, n.RelatedEntityId,
                n.ScheduledAt, n.SentAt, n.RetryCount, n.ErrorMessage, n.CreatedAt))
            .ToListAsync(ct);

        return Ok(new PagedResult<NotificationDto>(rows, total, pq.SafePage, pq.SafePageSize));
    }

    [HttpPost("scan-now")]
    public async Task<ActionResult<object>> ScanNow(CancellationToken ct)
    {
        var added = await _scanner.ScanAndEnqueueAsync(ct);
        return Ok(new { added });
    }

    [HttpPost("{id:long}/retry")]
    public async Task<ActionResult<NotificationDto>> Retry(long id, CancellationToken ct)
    {
        var n = await _db.NotificationOutbox.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (n is null) return NotFound();
        if (n.Status == NotificationStatus.Sent)
            return Conflict(new { code = "AlreadySent", message = "已发送，无需重试" });
        if (n.Status == NotificationStatus.Cancelled)
            return Conflict(new { code = "Cancelled", message = "已取消，请先恢复或新建一条" });

        var result = await _dispatcher.SendAsync(n, ct);
        if (result.Success)
        {
            n.Status = NotificationStatus.Sent;
            n.SentAt = _clock.UtcNow;
            n.ErrorMessage = null;
        }
        else
        {
            n.RetryCount++;
            var err = result.Error ?? "unknown";
            n.ErrorMessage = err.Length > 500 ? err[..500] : err;
        }
        await _db.SaveChangesAsync(ct);
        return Ok(MapDto(n));
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<ActionResult<NotificationDto>> Cancel(long id, CancellationToken ct)
    {
        var n = await _db.NotificationOutbox.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (n is null) return NotFound();
        if (n.Status == NotificationStatus.Sent)
            return Conflict(new { code = "AlreadySent", message = "已发送，不能取消" });
        n.Status = NotificationStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        return Ok(MapDto(n));
    }

    private static NotificationDto MapDto(MassageSaas.Domain.Entities.NotificationOutbox n) => new(
        n.Id, n.Kind.ToString(), n.Status.ToString(), n.DedupKey,
        n.MemberId, n.RecipientPhone, n.RecipientOpenId,
        n.Title, n.Body, n.RelatedEntityId,
        n.ScheduledAt, n.SentAt, n.RetryCount, n.ErrorMessage, n.CreatedAt);
}
