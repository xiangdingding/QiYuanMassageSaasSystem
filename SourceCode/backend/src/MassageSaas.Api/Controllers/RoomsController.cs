using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Rooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public RoomsController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> List([FromQuery] long storeId, [FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var roomsQ = _db.Rooms.AsNoTracking().Where(r => r.StoreId == storeId);
        if (!includeInactive) roomsQ = roomsQ.Where(r => r.IsActive);
        var rooms = await roomsQ.OrderBy(r => r.RoomNo).ToListAsync(ct);

        var roomIds = rooms.Select(r => r.Id).ToList();
        // 占用 = 当前有未结账（Pending/InProgress）订单的 OrderItem 引用
        var occupied = await _db.OrderItems.AsNoTracking()
            .Where(oi => oi.RoomId != null
                         && roomIds.Contains(oi.RoomId.Value)
                         && (oi.Order.Status == OrderStatus.Pending || oi.Order.Status == OrderStatus.InProgress))
            .Select(oi => new { RoomId = oi.RoomId!.Value, OrderId = oi.OrderId, oi.Order.OrderNo })
            .ToListAsync(ct);
        var occMap = occupied.GroupBy(x => x.RoomId).ToDictionary(g => g.Key, g => g.First());

        return Ok(rooms.Select(r => new RoomDto(
            r.Id, r.StoreId, r.RoomNo, r.Capacity, r.RoomType, r.Remark, r.IsActive,
            occMap.ContainsKey(r.Id),
            occMap.TryGetValue(r.Id, out var occ) ? occ.OrderId : null,
            occMap.TryGetValue(r.Id, out var occ2) ? occ2.OrderNo : null,
            r.IsTimedRoom, r.HourlyRate)).ToList());
    }

    [HttpPost]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<RoomDto>> Create([FromBody] CreateRoomRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.RoomNo)) return BadRequest(new { code = "InvalidInput", message = "房间号必填" });
        var dup = await _db.Rooms.AnyAsync(r => r.StoreId == req.StoreId && r.RoomNo == req.RoomNo, ct);
        if (dup) return Conflict(new { code = "Duplicate", message = "该门店已有同号房间" });

        if (req.IsTimedRoom && req.HourlyRate <= 0)
            return BadRequest(new { code = "InvalidRate", message = "计时房需设置大于 0 的小时单价" });

        var room = new Room
        {
            StoreId = req.StoreId,
            RoomNo = req.RoomNo.Trim(),
            Capacity = req.Capacity < 1 ? 1 : req.Capacity,
            RoomType = req.RoomType,
            Remark = req.Remark,
            IsTimedRoom = req.IsTimedRoom,
            HourlyRate = req.IsTimedRoom ? req.HourlyRate : 0m
        };
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync(ct);
        return Ok(new RoomDto(room.Id, room.StoreId, room.RoomNo, room.Capacity,
            room.RoomType, room.Remark, room.IsActive, false, null, null,
            room.IsTimedRoom, room.HourlyRate));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<RoomDto>> Update(long id, [FromBody] UpdateRoomRequest req, CancellationToken ct)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (room is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.RoomNo)) return BadRequest(new { code = "InvalidInput", message = "房间号必填" });

        if (req.RoomNo != room.RoomNo)
        {
            var dup = await _db.Rooms.AnyAsync(r => r.StoreId == room.StoreId && r.RoomNo == req.RoomNo && r.Id != id, ct);
            if (dup) return Conflict(new { code = "Duplicate", message = "该门店已有同号房间" });
        }

        if (req.IsTimedRoom && req.HourlyRate <= 0)
            return BadRequest(new { code = "InvalidRate", message = "计时房需设置大于 0 的小时单价" });

        room.RoomNo = req.RoomNo.Trim();
        room.Capacity = req.Capacity < 1 ? 1 : req.Capacity;
        room.RoomType = req.RoomType;
        room.Remark = req.Remark;
        room.IsActive = req.IsActive;
        room.IsTimedRoom = req.IsTimedRoom;
        room.HourlyRate = req.IsTimedRoom ? req.HourlyRate : 0m;
        await _db.SaveChangesAsync(ct);

        return Ok(new RoomDto(room.Id, room.StoreId, room.RoomNo, room.Capacity,
            room.RoomType, room.Remark, room.IsActive, false, null, null,
            room.IsTimedRoom, room.HourlyRate));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<IActionResult> Remove(long id, CancellationToken ct)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (room is null) return NotFound();
        var inUse = await _db.OrderItems.AnyAsync(oi => oi.RoomId == id
            && (oi.Order.Status == OrderStatus.Pending || oi.Order.Status == OrderStatus.InProgress), ct);
        if (inUse) return Conflict(new { code = "InUse", message = "房间被进行中订单占用，请改为停用" });
        room.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
