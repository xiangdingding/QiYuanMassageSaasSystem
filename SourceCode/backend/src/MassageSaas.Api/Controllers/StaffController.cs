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
                u.LastLoginAt, u.CreatedAt))
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
            IsActive = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return Ok(new StaffDto(
            user.Id, user.StoreId, user.Username, user.RealName, user.Phone,
            user.Role.ToString(), user.EmployeeNo, user.IsBlind, user.IsActive,
            user.LastLoginAt, user.CreatedAt));
    }

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
        await _db.SaveChangesAsync(ct);

        return Ok(new StaffDto(
            user.Id, user.StoreId, user.Username, user.RealName, user.Phone,
            user.Role.ToString(), user.EmployeeNo, user.IsBlind, user.IsActive,
            user.LastLoginAt, user.CreatedAt));
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
}
