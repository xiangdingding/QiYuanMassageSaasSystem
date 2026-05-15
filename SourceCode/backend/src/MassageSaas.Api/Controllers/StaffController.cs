using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Common;
using MassageSaas.Shared.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize(Policy = "ShopStaff")]
public class StaffController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public StaffController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<StaffDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? role = null,
        [FromQuery] long? storeId = null,
        [FromQuery] string? keyword = null,
        CancellationToken ct = default)
    {
        var pq = new PageQuery(page, pageSize, keyword);
        var q = _db.Users.AsNoTracking()
            .Where(u => u.Role != UserRole.PlatformAdmin);

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var r))
            q = q.Where(u => u.Role == r);
        if (storeId.HasValue) q = q.Where(u => u.StoreId == storeId.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(u => u.Username.Contains(k) || (u.RealName != null && u.RealName.Contains(k)) || (u.Phone != null && u.Phone.Contains(k)));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(u => u.Role).ThenBy(u => u.EmployeeNo).ThenBy(u => u.Id)
            .Skip((pq.SafePage - 1) * pq.SafePageSize)
            .Take(pq.SafePageSize)
            .Select(u => new StaffDto(
                u.Id, u.StoreId, u.Username, u.RealName, u.Phone,
                u.Role.ToString(), u.EmployeeNo, u.IsBlind, u.IsActive,
                u.LastLoginAt, u.CreatedAt,
                u.TechnicianLevel.ToString(), u.BlindCertNo, u.MaxRoundsPerDay, u.Specialties))
            .ToListAsync(ct);

        return Ok(new PagedResult<StaffDto>(items, total, pq.SafePage, pq.SafePageSize));
    }

    [HttpPost]
    public async Task<ActionResult<StaffDto>> Create([FromBody] CreateStaffRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { code = "InvalidInput", message = "用户名与密码必填" });
        if (req.Password.Length < 6)
            return BadRequest(new { code = "WeakPassword", message = "密码至少 6 位" });
        if (!Enum.TryParse<UserRole>(req.Role, true, out var role) || role == UserRole.PlatformAdmin)
            return BadRequest(new { code = "InvalidRole", message = "角色不合法" });

        var dup = await _db.Users.AnyAsync(u => u.Username == req.Username, ct);
        if (dup) return Conflict(new { code = "DuplicateUsername", message = "用户名已存在" });

        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == req.StoreId, ct);
        if (store is null) return BadRequest(new { code = "StoreNotFound", message = "门店不存在" });

        if (req.EmployeeNo.HasValue)
        {
            var dupNo = await _db.Users.AnyAsync(u =>
                u.StoreId == req.StoreId && u.EmployeeNo == req.EmployeeNo.Value, ct);
            if (dupNo) return Conflict(new { code = "DuplicateEmployeeNo", message = "工号已存在" });
        }

        var user = new User
        {
            StoreId = req.StoreId,
            Username = req.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            RealName = req.RealName?.Trim(),
            Phone = req.Phone?.Trim(),
            Role = role,
            EmployeeNo = req.EmployeeNo,
            IsBlind = req.IsBlind,
            IsActive = true,
            TechnicianLevel = ParseTechLevel(req.TechnicianLevel),
            BlindCertNo = req.BlindCertNo,
            MaxRoundsPerDay = req.MaxRoundsPerDay,
            Specialties = req.Specialties
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return Ok(MapDto(user));
    }

    private static TechnicianLevel ParseTechLevel(string? s) =>
        Enum.TryParse<TechnicianLevel>(s, true, out var v) ? v : TechnicianLevel.Senior;

    private static StaffDto MapDto(User u) => new(
        u.Id, u.StoreId, u.Username, u.RealName, u.Phone,
        u.Role.ToString(), u.EmployeeNo, u.IsBlind, u.IsActive,
        u.LastLoginAt, u.CreatedAt,
        u.TechnicianLevel.ToString(), u.BlindCertNo, u.MaxRoundsPerDay, u.Specialties);

    [HttpPut("{id:long}")]
    public async Task<ActionResult<StaffDto>> Update(long id, [FromBody] UpdateStaffRequest req, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role != UserRole.PlatformAdmin, ct);
        if (user is null) return NotFound();
        if (!Enum.TryParse<UserRole>(req.Role, true, out var role) || role == UserRole.PlatformAdmin)
            return BadRequest(new { code = "InvalidRole", message = "角色不合法" });

        user.StoreId = req.StoreId;
        user.Role = role;
        user.RealName = req.RealName?.Trim();
        user.Phone = req.Phone?.Trim();
        user.EmployeeNo = req.EmployeeNo;
        user.IsBlind = req.IsBlind;
        user.IsActive = req.IsActive;
        if (req.TechnicianLevel is { } lvl) user.TechnicianLevel = ParseTechLevel(lvl);
        user.BlindCertNo = req.BlindCertNo;
        user.MaxRoundsPerDay = req.MaxRoundsPerDay;
        user.Specialties = req.Specialties;
        await _db.SaveChangesAsync(ct);

        return Ok(MapDto(user));
    }

    [HttpPost("{id:long}/reset-password")]
    public async Task<IActionResult> ResetPassword(long id, [FromBody] ResetPasswordRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
            return BadRequest(new { code = "WeakPassword", message = "新密码至少 6 位" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role != UserRole.PlatformAdmin, ct);
        if (user is null) return NotFound();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---- 跨店调动 ----

    [HttpGet("transfers")]
    public async Task<ActionResult<IReadOnlyList<StaffTransferDto>>> ListTransfers(
        [FromQuery] long? userId = null,
        [FromQuery] long? storeId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var q = _db.StaffTransfers.AsNoTracking()
            .Include(t => t.User).Include(t => t.FromStore)
            .Include(t => t.ToStore).Include(t => t.OperatorUser)
            .AsQueryable();
        if (userId.HasValue) q = q.Where(t => t.UserId == userId.Value);
        if (storeId.HasValue) q = q.Where(t => t.FromStoreId == storeId.Value || t.ToStoreId == storeId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StaffTransferStatus>(status, true, out var st))
            q = q.Where(t => t.Status == st);

        var rows = await q.OrderByDescending(t => t.CreatedAt).Take(200).ToListAsync(ct);
        return Ok(rows.Select(MapTransfer).ToList());
    }

    [HttpPost("{id:long}/transfer")]
    public async Task<ActionResult<StaffTransferDto>> Transfer(
        long id, [FromBody] TransferStaffRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<StaffTransferKind>(req.Kind, true, out var kind))
            return BadRequest(new { code = "InvalidKind", message = "调动类型不合法" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role != UserRole.PlatformAdmin, ct);
        if (user is null) return NotFound();
        if (user.StoreId is not long fromStoreId)
            return BadRequest(new { code = "NoCurrentStore", message = "员工未绑定门店，无法调动" });
        if (fromStoreId == req.ToStoreId)
            return BadRequest(new { code = "SameStore", message = "目标门店与当前门店相同" });

        var toStore = await _db.Stores.FirstOrDefaultAsync(s => s.Id == req.ToStoreId, ct);
        if (toStore is null) return BadRequest(new { code = "StoreNotFound", message = "目标门店不存在" });
        if (kind == StaffTransferKind.Temporary && req.ExpectedReturnAt is null)
            return BadRequest(new { code = "MissingReturnDate", message = "临时借调需填预计归还时间" });

        var pendingNo = user.EmployeeNo.HasValue && await _db.Users.AnyAsync(u =>
            u.StoreId == req.ToStoreId && u.EmployeeNo == user.EmployeeNo && u.Id != user.Id, ct);
        if (pendingNo)
            return Conflict(new { code = "DuplicateEmployeeNo", message = "目标门店已有相同工号，请先调整工号" });

        var transfer = new StaffTransfer
        {
            UserId = user.Id,
            FromStoreId = fromStoreId,
            ToStoreId = req.ToStoreId,
            Kind = kind,
            Status = StaffTransferStatus.InEffect,
            EffectiveFrom = DateTime.UtcNow,
            ExpectedReturnAt = req.ExpectedReturnAt,
            Reason = req.Reason,
            OperatorUserId = _tenantContext.UserId
        };
        _db.StaffTransfers.Add(transfer);

        user.StoreId = req.ToStoreId;
        await ResetQueueForStoreChangeAsync(user.Id, req.ToStoreId, ct);

        await _db.SaveChangesAsync(ct);
        return Ok(await LoadTransferAsync(transfer.Id, ct));
    }

    [HttpPost("transfers/{transferId:long}/return")]
    public async Task<ActionResult<StaffTransferDto>> ReturnTransfer(long transferId, CancellationToken ct)
    {
        var transfer = await _db.StaffTransfers.FirstOrDefaultAsync(t => t.Id == transferId, ct);
        if (transfer is null) return NotFound();
        if (transfer.Kind != StaffTransferKind.Temporary)
            return Conflict(new { code = "NotTemporary", message = "仅临时借调可归还，永久调动不可逆" });
        if (transfer.Status != StaffTransferStatus.InEffect)
            return Conflict(new { code = "InvalidState", message = "该调动不在生效中" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == transfer.UserId, ct);
        if (user is null) return NotFound(new { code = "UserNotFound", message = "员工不存在" });

        transfer.Status = StaffTransferStatus.Returned;
        transfer.ReturnedAt = DateTime.UtcNow;
        user.StoreId = transfer.FromStoreId;
        await ResetQueueForStoreChangeAsync(user.Id, transfer.FromStoreId, ct);

        await _db.SaveChangesAsync(ct);
        return Ok(await LoadTransferAsync(transfer.Id, ct));
    }

    /// <summary>员工换店后，把其叫号队列迁到新店并置为下班（需在新店重新上钟）。</summary>
    private async Task ResetQueueForStoreChangeAsync(long userId, long newStoreId, CancellationToken ct)
    {
        var queue = await _db.TechnicianQueues.FirstOrDefaultAsync(q => q.TechnicianId == userId, ct);
        if (queue is null) return;
        queue.StoreId = newStoreId;
        queue.State = QueueState.OffDuty;
        queue.QueuePosition = 0;
        queue.EnteredAt = null;
        queue.TodayRoundCount = 0;
        queue.LastCalledAt = null;
    }

    private async Task<StaffTransferDto> LoadTransferAsync(long id, CancellationToken ct)
    {
        var t = await _db.StaffTransfers.AsNoTracking()
            .Include(x => x.User).Include(x => x.FromStore)
            .Include(x => x.ToStore).Include(x => x.OperatorUser)
            .FirstAsync(x => x.Id == id, ct);
        return MapTransfer(t);
    }

    private static StaffTransferDto MapTransfer(StaffTransfer t) => new(
        t.Id, t.UserId, t.User?.RealName ?? t.User?.Username ?? string.Empty,
        t.FromStoreId, t.FromStore?.Name ?? string.Empty,
        t.ToStoreId, t.ToStore?.Name ?? string.Empty,
        t.Kind.ToString(), t.Status.ToString(),
        t.EffectiveFrom, t.ExpectedReturnAt, t.ReturnedAt,
        t.Reason, t.OperatorUser?.RealName ?? t.OperatorUser?.Username,
        t.CreatedAt);
}
