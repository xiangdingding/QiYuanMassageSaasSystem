using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Schedules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/schedules")]
[Authorize]
public class SchedulesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public SchedulesController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StaffScheduleDto>>> List(
        [FromQuery] long storeId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var fromDate = from.HasValue ? DateOnly.FromDateTime(from.Value) : DateOnly.FromDateTime(DateTime.UtcNow);
        var toDate = to.HasValue ? DateOnly.FromDateTime(to.Value) : fromDate.AddDays(14);
        var rows = await _db.StaffSchedules.AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.StoreId == storeId && s.WorkDate >= fromDate && s.WorkDate <= toDate)
            .OrderBy(s => s.WorkDate).ThenBy(s => s.StartTime)
            .ToListAsync(ct);
        return Ok(rows.Select(MapDto).ToList());
    }

    [HttpPost]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<StaffScheduleDto>> Create([FromBody] CreateStaffScheduleRequest req, CancellationToken ct)
    {
        if (!TimeOnly.TryParse(req.StartTime, out var start) || !TimeOnly.TryParse(req.EndTime, out var end))
            return BadRequest(new { code = "InvalidTime", message = "时间格式不合法，需为 HH:mm" });
        if (end <= start)
            return BadRequest(new { code = "InvalidRange", message = "下班时间必须晚于上班时间" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId && u.IsActive, ct);
        if (user is null) return BadRequest(new { code = "UserNotFound", message = "员工不存在或已停用" });

        var workDate = DateOnly.FromDateTime(req.WorkDate);
        // 同人同日唯一（唯一索引 StoreId+WorkDate+UserId，不区分软删除）。
        // 采用 Upsert：已存在（含被软删除的）就直接更新为新班次，不再报"该日已存在该员工的排班"。
        // 这样新增排班永远成功（创建或更新），CS / BS 行为完全一致。
        var existing = await _db.StaffSchedules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.StoreId == req.StoreId && s.UserId == req.UserId && s.WorkDate == workDate, ct);
        if (existing is not null)
        {
            existing.IsDeleted = false;
            existing.StartTime = start;
            existing.EndTime = end;
            existing.Remark = req.Remark;
            await _db.SaveChangesAsync(ct);
            await _db.Entry(existing).Reference(s => s.User).LoadAsync(ct);
            return Ok(MapDto(existing));
        }

        var entity = new StaffSchedule
        {
            StoreId = req.StoreId,
            UserId = req.UserId,
            WorkDate = workDate,
            StartTime = start,
            EndTime = end,
            Remark = req.Remark
        };
        _db.StaffSchedules.Add(entity);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(entity).Reference(s => s.User).LoadAsync(ct);
        return Ok(MapDto(entity));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var s = await _db.StaffSchedules.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return NotFound();
        s.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("leaves")]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> Leaves(
        [FromQuery] long? userId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var q = _db.LeaveRequests.AsNoTracking()
            .Include(l => l.User)
            .Include(l => l.ApproverUser)
            .AsQueryable();
        if (userId.HasValue) q = q.Where(l => l.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeaveStatus>(status, true, out var st))
            q = q.Where(l => l.Status == st);
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<LeaveType>(type, true, out var ty))
            q = q.Where(l => l.Type == ty);
        // 日期按区间重叠过滤（请假跨度 [FromDate, ToDate] 与 [from, to] 有交集即命中）
        if (from.HasValue)
        {
            var f = DateOnly.FromDateTime(from.Value);
            q = q.Where(l => l.ToDate >= f);
        }
        if (to.HasValue)
        {
            var t = DateOnly.FromDateTime(to.Value);
            q = q.Where(l => l.FromDate <= t);
        }
        var rows = await q.OrderByDescending(l => l.CreatedAt).Take(200).ToListAsync(ct);
        return Ok(rows.Select(MapLeave).ToList());
    }

    [HttpPost("leaves")]
    public async Task<ActionResult<LeaveRequestDto>> SubmitLeave([FromBody] CreateLeaveRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<LeaveType>(req.Type, true, out var type))
            return BadRequest(new { code = "InvalidType", message = "请假类型不合法" });
        if (req.ToDate < req.FromDate)
            return BadRequest(new { code = "InvalidRange", message = "结束日期不能早于开始日期" });
        if (!Enum.TryParse<DayHalf>(req.StartHalf, true, out var startHalf)
            || !Enum.TryParse<DayHalf>(req.EndHalf, true, out var endHalf))
            return BadRequest(new { code = "InvalidHalf", message = "上午/下午时段不合法" });

        var fromDate = DateOnly.FromDateTime(req.FromDate);
        var toDate = DateOnly.FromDateTime(req.ToDate);
        if (LeaveDayMath.Compute(fromDate, toDate, startHalf, endHalf) <= 0m)
            return BadRequest(new { code = "InvalidRange", message = "请假时长须大于 0（同日不能从下午请到上午）" });

        // 技师只能给自己请假；店长/收银员可代员工提交
        if (_tenantContext.UserId is not long uid) return Unauthorized();
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role == nameof(UserRole.Technician) && req.UserId != uid)
            return Forbid();

        var leave = new LeaveRequest
        {
            UserId = req.UserId,
            Type = type,
            FromDate = fromDate,
            ToDate = toDate,
            StartHalf = startHalf,
            EndHalf = endHalf,
            Reason = req.Reason,
            Status = LeaveStatus.Pending
        };
        _db.LeaveRequests.Add(leave);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(leave).Reference(l => l.User).LoadAsync(ct);
        return Ok(MapLeave(leave));
    }

    [HttpPost("leaves/{id:long}/approve")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<LeaveRequestDto>> Approve(long id, [FromBody] ApproveLeaveRequest req, CancellationToken ct)
    {
        var leave = await _db.LeaveRequests.Include(l => l.User).FirstOrDefaultAsync(l => l.Id == id, ct);
        if (leave is null) return NotFound();
        if (leave.Status != LeaveStatus.Pending)
            return Conflict(new { code = "InvalidState", message = "已处理的请假不可重审" });

        leave.Status = req.Approve ? LeaveStatus.Approved : LeaveStatus.Rejected;
        leave.ApproverUserId = _tenantContext.UserId;
        leave.ApprovedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(req.Reason))
            leave.Reason = (leave.Reason ?? string.Empty) + " | 审批: " + req.Reason;

        // 批准后：如果涉及今天，把对应技师 Queue 强制置下班
        if (req.Approve)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (leave.FromDate <= today && today <= leave.ToDate)
            {
                var queue = await _db.TechnicianQueues.FirstOrDefaultAsync(q => q.TechnicianId == leave.UserId, ct);
                if (queue is not null)
                {
                    queue.State = QueueState.OffDuty;
                    queue.EnteredAt = null;
                    queue.QueuePosition = 0;
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        await _db.Entry(leave).Reference(l => l.ApproverUser!).LoadAsync(ct);
        return Ok(MapLeave(leave));
    }

    private static StaffScheduleDto MapDto(StaffSchedule s) => new(
        s.Id, s.StoreId, s.UserId,
        s.User?.RealName ?? s.User?.Username ?? string.Empty,
        s.WorkDate.ToDateTime(TimeOnly.MinValue),
        s.StartTime.ToString("HH:mm"),
        s.EndTime.ToString("HH:mm"),
        s.Remark);

    private static LeaveRequestDto MapLeave(LeaveRequest l) => new(
        l.Id, l.UserId,
        l.User?.RealName ?? l.User?.Username ?? string.Empty,
        l.Type.ToString(),
        l.FromDate.ToDateTime(TimeOnly.MinValue),
        l.ToDate.ToDateTime(TimeOnly.MinValue),
        l.StartHalf.ToString(),
        l.EndHalf.ToString(),
        LeaveDayMath.Compute(l.FromDate, l.ToDate, l.StartHalf, l.EndHalf),
        l.Reason, l.Status.ToString(),
        l.ApproverUserId,
        l.ApproverUser != null ? (l.ApproverUser.RealName ?? l.ApproverUser.Username) : null,
        l.ApprovedAt, l.CreatedAt);
}
