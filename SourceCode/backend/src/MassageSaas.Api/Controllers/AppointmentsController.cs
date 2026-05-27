using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Appointments;
using MassageSaas.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        ApplicationDbContext db,
        ITenantContext tenantContext,
        ILogger<AppointmentsController> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// 顾客（小程序）下单。匿名可用：通过 storeId + tenant 解析，绕过租户过滤。
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<AppointmentDto>> Create([FromBody] CreateAppointmentRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.CustomerName) || string.IsNullOrWhiteSpace(req.CustomerPhone))
            return BadRequest(new { code = "InvalidInput", message = "请填写姓名与手机号" });
        if (req.ExpectedArriveAt < DateTime.UtcNow.AddMinutes(-5))
            return BadRequest(new { code = "InvalidInput", message = "到店时间不能早于当前时间" });
        if (req.PartySize < 1 || req.PartySize > 20)
            return BadRequest(new { code = "InvalidInput", message = "人数应在 1-20 之间" });

        _tenantContext.BypassTenantFilter();

        var store = await _db.Stores
            .Where(s => s.Id == req.StoreId && s.IsActive)
            .Select(s => new { s.Id, s.TenantId, s.Name })
            .FirstOrDefaultAsync(ct);
        if (store is null) return BadRequest(new { code = "StoreNotFound", message = "门店不存在或已停用" });

        if (req.ServiceId.HasValue)
        {
            var serviceExists = await _db.ServiceItems
                .AnyAsync(s => s.Id == req.ServiceId.Value && s.TenantId == store.TenantId && s.IsActive, ct);
            if (!serviceExists) return BadRequest(new { code = "ServiceNotFound", message = "服务项不存在" });
        }

        if (req.PreferredTechnicianId.HasValue)
        {
            var techExists = await _db.Users.AnyAsync(u =>
                u.Id == req.PreferredTechnicianId.Value &&
                u.TenantId == store.TenantId &&
                u.Role == UserRole.Technician && u.IsActive, ct);
            if (!techExists) return BadRequest(new { code = "TechnicianNotFound", message = "技师不存在或已停用" });
        }

        var appointment = new Appointment
        {
            TenantId = store.TenantId,
            StoreId = store.Id,
            ServiceId = req.ServiceId,
            PreferredTechnicianId = req.PreferredTechnicianId,
            CustomerName = req.CustomerName.Trim(),
            CustomerPhone = req.CustomerPhone.Trim(),
            CustomerOpenId = req.CustomerOpenId,
            ExpectedArriveAt = req.ExpectedArriveAt,
            PartySize = req.PartySize,
            Remark = req.Remark,
            Status = AppointmentStatus.Pending
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Appointment created store={StoreId} phone={Phone} arrive={Arrive:O}",
            store.Id, appointment.CustomerPhone, appointment.ExpectedArriveAt);

        var dto = await LoadDtoAsync(appointment.Id, ct);
        return CreatedAtAction(nameof(Get), new { id = appointment.Id }, dto);
    }

    /// <summary>顾客查询自己历史预约（小程序通过 openId 查），无需登录。</summary>
    [HttpGet("by-customer")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> ListByCustomer(
        [FromQuery] string? openId,
        [FromQuery] string? phone,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(openId) && string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { code = "InvalidInput", message = "需要提供 openId 或手机号" });

        _tenantContext.BypassTenantFilter();
        var q = _db.Appointments.AsNoTracking()
            .Include(a => a.Store)
            .Include(a => a.Service)
            .Include(a => a.PreferredTechnician)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(openId))
            q = q.Where(a => a.CustomerOpenId == openId);
        else
            q = q.Where(a => a.CustomerPhone == phone);

        var rows = await q.OrderByDescending(a => a.ExpectedArriveAt).Take(50).ToListAsync(ct);
        return Ok(rows.Select(ToDto).ToList());
    }

    [HttpGet]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<PagedResult<AppointmentDto>>> List(
        [FromQuery] long? storeId,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var q = _db.Appointments.AsNoTracking()
            .Include(a => a.Store)
            .Include(a => a.Service)
            .Include(a => a.PreferredTechnician)
            .AsQueryable();
        if (storeId.HasValue) q = q.Where(a => a.StoreId == storeId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AppointmentStatus>(status, true, out var s))
            q = q.Where(a => a.Status == s);
        if (from.HasValue) q = q.Where(a => a.ExpectedArriveAt >= from.Value);
        if (to.HasValue) q = q.Where(a => a.ExpectedArriveAt < to.Value);

        var pq = new PageQuery(page, pageSize, null);
        var total = await q.CountAsync(ct);
        // 已取消的统一沉底，让前台一眼看到待处理的；其它状态按到店时间正排
        var rows = await q
            .OrderBy(a => a.Status == AppointmentStatus.Cancelled ? 1 : 0)
            .ThenBy(a => a.ExpectedArriveAt)
            .Skip((pq.SafePage - 1) * pq.SafePageSize)
            .Take(pq.SafePageSize)
            .ToListAsync(ct);
        return Ok(new PagedResult<AppointmentDto>(rows.Select(ToDto).ToList(), total, pq.SafePage, pq.SafePageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AppointmentDto>> Get(long id, CancellationToken ct)
    {
        _tenantContext.BypassTenantFilter();
        var dto = await LoadDtoAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>
    /// 修改预约：仅 Pending（未确认）允许改；Confirmed 之后改时间须先取消重排，避免和已通知客人的口径出现不一致。
    /// </summary>
    [HttpPut("{id:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<AppointmentDto>> Update(long id, [FromBody] UpdateAppointmentRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.CustomerName) || string.IsNullOrWhiteSpace(req.CustomerPhone))
            return BadRequest(new { code = "InvalidInput", message = "请填写姓名与手机号" });
        if (req.ExpectedArriveAt < DateTime.UtcNow.AddMinutes(-5))
            return BadRequest(new { code = "InvalidInput", message = "到店时间不能早于当前时间" });
        if (req.PartySize < 1 || req.PartySize > 20)
            return BadRequest(new { code = "InvalidInput", message = "人数应在 1-20 之间" });

        var a = await _db.Appointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return NotFound();
        if (a.Status != AppointmentStatus.Pending)
            return Conflict(new { code = "InvalidState", message = "仅未确认（待确认）的预约可修改" });

        if (req.ServiceId.HasValue)
        {
            var serviceOk = await _db.ServiceItems
                .AnyAsync(s => s.Id == req.ServiceId.Value && s.IsActive, ct);
            if (!serviceOk) return BadRequest(new { code = "ServiceNotFound", message = "服务项不存在" });
        }
        if (req.PreferredTechnicianId.HasValue)
        {
            var techOk = await _db.Users.AnyAsync(u =>
                u.Id == req.PreferredTechnicianId.Value &&
                u.Role == UserRole.Technician && u.IsActive, ct);
            if (!techOk) return BadRequest(new { code = "TechnicianNotFound", message = "技师不存在或已停用" });
        }

        a.ServiceId = req.ServiceId;
        a.PreferredTechnicianId = req.PreferredTechnicianId;
        a.CustomerName = req.CustomerName.Trim();
        a.CustomerPhone = req.CustomerPhone.Trim();
        a.ExpectedArriveAt = req.ExpectedArriveAt;
        a.PartySize = req.PartySize;
        a.Remark = req.Remark;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Appointment {Id} updated by user={UserId}", id, _tenantContext.UserId);
        return Ok(await LoadDtoAsync(id, ct));
    }

    [HttpPost("{id:long}/confirm")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<AppointmentDto>> Confirm(long id, [FromBody] ConfirmAppointmentRequest req, CancellationToken ct)
    {
        var a = await _db.Appointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return NotFound();
        if (a.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed or AppointmentStatus.NoShow)
            return Conflict(new { code = "InvalidState", message = $"当前状态 {a.Status} 不可确认" });

        a.Status = AppointmentStatus.Confirmed;
        a.ConfirmedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(req.Remark))
            a.Remark = (a.Remark ?? string.Empty) + " | 确认备注: " + req.Remark;
        await _db.SaveChangesAsync(ct);
        return Ok(await LoadDtoAsync(id, ct));
    }

    [HttpPost("{id:long}/arrive")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<AppointmentDto>> Arrive(long id, CancellationToken ct)
    {
        var a = await _db.Appointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return NotFound();
        if (a.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed or AppointmentStatus.NoShow)
            return Conflict(new { code = "InvalidState", message = $"当前状态 {a.Status} 不可签到" });
        a.Status = AppointmentStatus.Arrived;
        a.ArrivedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(await LoadDtoAsync(id, ct));
    }

    /// <summary>
    /// 取消：店员或顾客（携带 openId/手机号匹配即可）。
    /// </summary>
    [HttpPost("{id:long}/cancel")]
    [AllowAnonymous]
    public async Task<ActionResult<AppointmentDto>> Cancel(
        long id,
        [FromBody] CancelAppointmentRequest req,
        [FromQuery] string? openId,
        [FromQuery] string? phone,
        CancellationToken ct)
    {
        _tenantContext.BypassTenantFilter();
        var a = await _db.Appointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return NotFound();

        var isStaff = User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value);
        if (!isStaff)
        {
            // 匿名取消必须自证：openId 或手机号匹配
            var ok = (!string.IsNullOrEmpty(openId) && a.CustomerOpenId == openId)
                  || (!string.IsNullOrEmpty(phone) && a.CustomerPhone == phone);
            if (!ok) return Forbid();
        }

        if (a.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed or AppointmentStatus.NoShow)
            return Conflict(new { code = "InvalidState", message = $"当前状态 {a.Status} 不可取消" });

        a.Status = AppointmentStatus.Cancelled;
        a.CancelledAt = DateTime.UtcNow;
        a.CancelReason = req.Reason;
        await _db.SaveChangesAsync(ct);
        return Ok(await LoadDtoAsync(id, ct));
    }

    private async Task<AppointmentDto?> LoadDtoAsync(long id, CancellationToken ct)
    {
        var a = await _db.Appointments.AsNoTracking()
            .Include(x => x.Store)
            .Include(x => x.Service)
            .Include(x => x.PreferredTechnician)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return a is null ? null : ToDto(a);
    }

    private static AppointmentDto ToDto(Appointment a) => new(
        a.Id, a.StoreId, a.Store?.Name ?? string.Empty,
        a.ServiceId, a.Service?.Name,
        a.PreferredTechnicianId,
        a.PreferredTechnician != null ? (a.PreferredTechnician.RealName ?? a.PreferredTechnician.Username) : null,
        a.CustomerName, a.CustomerPhone,
        a.ExpectedArriveAt, a.PartySize,
        a.Status.ToString(), a.Remark, a.CreatedAt,
        a.ConfirmedAt, a.ArrivedAt, a.CancelledAt, a.CancelReason);
}
