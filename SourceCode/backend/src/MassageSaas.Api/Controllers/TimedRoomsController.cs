using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Rooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

/// <summary>
/// 计时房：客人进房开始计时，离开按停留时长 × 房间小时单价结算。
/// 与服务订单平行，结算后由报表/日结按 SettledAt 归集。
/// </summary>
[ApiController]
[Route("api/timed-rooms")]
[Authorize(Policy = "ShopStaff")]
public class TimedRoomsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TimedRoomsController> _logger;

    public TimedRoomsController(
        ApplicationDbContext db,
        ITenantContext tenantContext,
        ILogger<TimedRoomsController> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyList<TimedRoomSessionDto>>> ListSessions(
        [FromQuery] long storeId,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var q = _db.TimedRoomSessions.AsNoTracking()
            .Include(s => s.Room).Include(s => s.Member).Include(s => s.OperatorUser)
            .Where(s => s.StoreId == storeId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TimedRoomSessionStatus>(status, true, out var st))
            q = q.Where(s => s.Status == st);
        if (from.HasValue) q = q.Where(s => s.StartedAt >= from.Value);
        if (to.HasValue) q = q.Where(s => s.StartedAt < to.Value);

        var rows = await q.OrderByDescending(s => s.StartedAt).Take(200).ToListAsync(ct);
        return Ok(rows.Select(MapDto).ToList());
    }

    [HttpPost("{roomId:long}/start")]
    public async Task<ActionResult<TimedRoomSessionDto>> Start(
        long roomId, [FromBody] StartTimedRoomRequest req, CancellationToken ct)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId, ct);
        if (room is null) return NotFound(new { code = "RoomNotFound", message = "房间不存在" });
        if (!room.IsTimedRoom)
            return BadRequest(new { code = "NotTimedRoom", message = "该房间不是计时房" });
        if (!room.IsActive)
            return BadRequest(new { code = "RoomInactive", message = "房间已停用" });
        if (room.HourlyRate <= 0)
            return BadRequest(new { code = "InvalidRate", message = "房间未设置小时单价" });

        var occupied = await _db.TimedRoomSessions.AnyAsync(s =>
            s.RoomId == roomId && s.Status == TimedRoomSessionStatus.Open, ct);
        if (occupied) return Conflict(new { code = "RoomBusy", message = "该计时房已有进行中的计时" });

        if (req.MemberId is long mid)
        {
            var memberOk = await _db.Members.AnyAsync(m => m.Id == mid && m.IsActive, ct);
            if (!memberOk) return BadRequest(new { code = "MemberNotFound", message = "会员不存在或已停用" });
        }

        var session = new TimedRoomSession
        {
            StoreId = room.StoreId,
            RoomId = room.Id,
            MemberId = req.MemberId,
            CustomerName = string.IsNullOrWhiteSpace(req.CustomerName) ? null : req.CustomerName.Trim(),
            StartedAt = DateTime.UtcNow,
            HourlyRateSnapshot = room.HourlyRate,
            Status = TimedRoomSessionStatus.Open,
            PayMethod = PayMethod.Unpaid,
            OperatorUserId = _tenantContext.UserId,
            Remark = req.Remark
        };
        _db.TimedRoomSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Timed room session started session={Id} room={Room}", session.Id, roomId);
        return Ok(await LoadAsync(session.Id, ct));
    }

    [HttpPost("sessions/{id:long}/stop")]
    public async Task<ActionResult<TimedRoomSessionDto>> Stop(
        long id, [FromBody] StopTimedRoomRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<PayMethod>(req.PayMethod, true, out var payMethod) || payMethod == PayMethod.Unpaid)
            return BadRequest(new { code = "InvalidPayMethod", message = "支付方式不合法" });

        var session = await _db.TimedRoomSessions.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (session is null) return NotFound();
        if (session.Status != TimedRoomSessionStatus.Open)
            return Conflict(new { code = "NotOpen", message = "该计时记录已结算或已作废" });

        var endedAt = DateTime.UtcNow;
        // 向上取整到分钟，至少计 1 分钟，避免 0 元
        var minutes = Math.Max(1, (int)Math.Ceiling((endedAt - session.StartedAt).TotalMinutes));
        var amount = Math.Round(minutes / 60m * session.HourlyRateSnapshot, 2, MidpointRounding.AwayFromZero);

        // 会员卡结算需扣余额，与订单结账口径一致
        if (payMethod == PayMethod.MemberCard)
        {
            if (session.MemberId is not long memberId)
                return BadRequest(new { code = "NoMember", message = "未关联会员，无法用会员卡结算" });
            var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == memberId, ct);
            if (member is null) return BadRequest(new { code = "MemberNotFound", message = "会员不存在" });
            if (member.Balance < amount)
                return BadRequest(new { code = "InsufficientBalance", message = $"会员余额不足（应付 ¥{amount:F2}，余额 ¥{member.Balance:F2}）" });
            member.Balance -= amount;
            member.TotalConsumed += amount;
        }
        else if (session.MemberId is long mid)
        {
            var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == mid, ct);
            if (member is not null) member.TotalConsumed += amount;
        }

        session.EndedAt = endedAt;
        session.BilledMinutes = minutes;
        session.Amount = amount;
        session.PayMethod = payMethod;
        session.Status = TimedRoomSessionStatus.Settled;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Timed room session settled session={Id} minutes={Min} amount={Amount}",
            session.Id, minutes, amount);
        return Ok(await LoadAsync(session.Id, ct));
    }

    [HttpPost("sessions/{id:long}/cancel")]
    public async Task<ActionResult<TimedRoomSessionDto>> Cancel(long id, CancellationToken ct)
    {
        var session = await _db.TimedRoomSessions.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (session is null) return NotFound();
        if (session.Status != TimedRoomSessionStatus.Open)
            return Conflict(new { code = "NotOpen", message = "只有进行中的计时可作废" });
        session.Status = TimedRoomSessionStatus.Cancelled;
        session.EndedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(await LoadAsync(session.Id, ct));
    }

    private async Task<TimedRoomSessionDto> LoadAsync(long id, CancellationToken ct)
    {
        var s = await _db.TimedRoomSessions.AsNoTracking()
            .Include(x => x.Room).Include(x => x.Member).Include(x => x.OperatorUser)
            .FirstAsync(x => x.Id == id, ct);
        return MapDto(s);
    }

    private static TimedRoomSessionDto MapDto(TimedRoomSession s)
    {
        var until = s.EndedAt ?? DateTime.UtcNow;
        var elapsed = Math.Max(0, (int)Math.Ceiling((until - s.StartedAt).TotalMinutes));
        return new TimedRoomSessionDto(
            s.Id, s.StoreId, s.RoomId, s.Room?.RoomNo ?? string.Empty,
            s.MemberId, s.Member?.Name ?? s.Member?.CardNo,
            s.CustomerName,
            s.StartedAt, s.EndedAt,
            s.HourlyRateSnapshot, s.BilledMinutes, elapsed,
            s.Amount, s.PayMethod.ToString(), s.Status.ToString(),
            s.OperatorUser?.RealName ?? s.OperatorUser?.Username,
            s.Remark);
    }
}
