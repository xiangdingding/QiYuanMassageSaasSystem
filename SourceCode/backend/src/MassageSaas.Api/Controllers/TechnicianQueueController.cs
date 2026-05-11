using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Queue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/queue")]
[Authorize]
public class TechnicianQueueController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public TechnicianQueueController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TechnicianQueueItemDto>>> List(
        [FromQuery] long storeId,
        CancellationToken ct = default)
    {
        var rows = await _db.TechnicianQueues.AsNoTracking()
            .Include(q => q.Technician)
            .Where(q => q.StoreId == storeId)
            .OrderBy(q => q.QueuePosition)
            .ThenBy(q => q.Id)
            .Select(q => new TechnicianQueueItemDto(
                q.Id, q.TechnicianId,
                q.Technician.RealName ?? q.Technician.Username,
                q.Technician.EmployeeNo,
                q.State.ToString(),
                q.QueuePosition, q.TodayRoundCount,
                q.EnteredAt, q.LastCalledAt))
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost("{technicianId:long}/state")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<IActionResult> SetState(long technicianId, [FromBody] SetQueueStateRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<QueueState>(req.State, true, out var state))
            return BadRequest(new { code = "InvalidState", message = "状态值不合法" });

        var tech = await _db.Users.FirstOrDefaultAsync(u =>
            u.Id == technicianId && u.Role == UserRole.Technician, ct);
        if (tech is null) return NotFound(new { code = "TechnicianNotFound", message = "技师不存在" });
        if (tech.StoreId is null)
            return BadRequest(new { code = "TechnicianNoStore", message = "技师未绑定门店" });

        var q = await _db.TechnicianQueues.FirstOrDefaultAsync(x =>
            x.TenantId == _tenantContext.TenantId && x.TechnicianId == technicianId, ct);
        if (q is null)
        {
            q = new TechnicianQueue
            {
                TechnicianId = technicianId,
                StoreId = tech.StoreId.Value,
                State = QueueState.OffDuty
            };
            _db.TechnicianQueues.Add(q);
        }

        q.State = state;
        if (state == QueueState.OnDuty)
        {
            q.EnteredAt ??= DateTime.UtcNow;
            if (q.QueuePosition == 0)
            {
                var max = await _db.TechnicianQueues
                    .Where(x => x.StoreId == q.StoreId)
                    .MaxAsync(x => (int?)x.QueuePosition, ct) ?? 0;
                q.QueuePosition = max + 1;
            }
        }
        else if (state == QueueState.OffDuty)
        {
            q.EnteredAt = null;
            q.QueuePosition = 0;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("call-next")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<CallNextResultDto>> CallNext([FromBody] CallNextRequest req, CancellationToken ct)
    {
        var idleQueue = await _db.TechnicianQueues
            .Include(q => q.Technician)
            .Where(q => q.StoreId == req.StoreId && q.State == QueueState.OnDuty)
            .OrderBy(q => q.TodayRoundCount)
            .ThenBy(q => q.LastCalledAt ?? q.EnteredAt ?? q.CreatedAt)
            .ThenBy(q => q.QueuePosition)
            .FirstOrDefaultAsync(ct);

        if (idleQueue is null)
            return Ok(new CallNextResultDto(null, null, null, 0));

        idleQueue.LastCalledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new CallNextResultDto(
            idleQueue.TechnicianId,
            idleQueue.Technician.RealName ?? idleQueue.Technician.Username,
            idleQueue.Technician.EmployeeNo,
            idleQueue.QueuePosition));
    }

    /// <summary>
    /// 当前登录技师查看自己的排队/上钟状态 + 正在服务的房间号。
    /// 给小程序"我的班次"用。
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<MyQueueDto>> Me(CancellationToken ct)
    {
        if (_tenantContext.UserId is not long uid)
            return Unauthorized();

        var q = await _db.TechnicianQueues.AsNoTracking()
            .Where(x => x.TechnicianId == uid)
            .Select(x => new
            {
                x.Id, x.TechnicianId, x.State,
                x.QueuePosition, x.TodayRoundCount,
                x.EnteredAt, x.LastCalledAt
            })
            .FirstOrDefaultAsync(ct);

        // 当前在服务的项（最近一条 InProgress 订单的本技师项），房间号取自 RoomNoSnapshot
        var nowUtc = DateTime.UtcNow;
        var currentItem = await _db.OrderItems.AsNoTracking()
            .Where(oi => oi.TechnicianId == uid
                         && (oi.Order.Status == OrderStatus.Pending || oi.Order.Status == OrderStatus.InProgress))
            .OrderByDescending(oi => oi.Order.CreatedAt)
            .Select(oi => new
            {
                oi.OrderId, oi.RoomNoSnapshot, oi.ServiceName
            })
            .FirstOrDefaultAsync(ct);

        if (q is null)
        {
            // 即使没有排队记录，也告诉技师"未排班"，便于小程序显示
            return Ok(new MyQueueDto(null, uid, QueueState.OffDuty.ToString(), 0, 0, null, null,
                currentItem?.RoomNoSnapshot, currentItem?.OrderId, currentItem?.ServiceName));
        }

        return Ok(new MyQueueDto(
            q.Id, q.TechnicianId, q.State.ToString(),
            q.QueuePosition, q.TodayRoundCount,
            q.EnteredAt, q.LastCalledAt,
            currentItem?.RoomNoSnapshot, currentItem?.OrderId, currentItem?.ServiceName));
    }

    /// <summary>
    /// 技师自助上钟/休息/下班。给小程序"我的班次"上的快捷按钮用，
    /// 与店员策略 [Authorize(Policy="ShopStaff")] 的 SetState 区分。
    /// </summary>
    [HttpPost("me/state")]
    public async Task<IActionResult> SetMyState([FromBody] SetQueueStateRequest req, CancellationToken ct)
    {
        if (_tenantContext.UserId is not long uid) return Unauthorized();
        if (!Enum.TryParse<QueueState>(req.State, true, out var state))
            return BadRequest(new { code = "InvalidState", message = "状态值不合法" });

        var tech = await _db.Users.FirstOrDefaultAsync(u =>
            u.Id == uid && u.Role == UserRole.Technician && u.IsActive, ct);
        if (tech is null) return Forbid();
        if (tech.StoreId is null) return BadRequest(new { code = "NoStore", message = "未绑定门店，请联系店长" });

        var queue = await _db.TechnicianQueues.FirstOrDefaultAsync(x =>
            x.TenantId == _tenantContext.TenantId && x.TechnicianId == uid, ct);
        if (queue is null)
        {
            queue = new TechnicianQueue
            {
                TechnicianId = uid,
                StoreId = tech.StoreId.Value,
                State = QueueState.OffDuty
            };
            _db.TechnicianQueues.Add(queue);
        }

        queue.State = state;
        if (state == QueueState.OnDuty)
        {
            queue.EnteredAt ??= DateTime.UtcNow;
            if (queue.QueuePosition == 0)
            {
                var max = await _db.TechnicianQueues
                    .Where(x => x.StoreId == queue.StoreId)
                    .MaxAsync(x => (int?)x.QueuePosition, ct) ?? 0;
                queue.QueuePosition = max + 1;
            }
        }
        else if (state == QueueState.OffDuty)
        {
            queue.EnteredAt = null;
            queue.QueuePosition = 0;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("reset-day")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<IActionResult> ResetDay([FromQuery] long storeId, CancellationToken ct)
    {
        var rows = await _db.TechnicianQueues
            .Where(q => q.StoreId == storeId)
            .ToListAsync(ct);
        foreach (var r in rows)
        {
            r.TodayRoundCount = 0;
            r.LastCalledAt = null;
        }
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
