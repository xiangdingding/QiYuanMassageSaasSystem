using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Auth;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext db,
        ITokenService tokenService,
        ITenantContext tenantContext,
        ILogger<AuthController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        _tenantContext.BypassTenantFilter();

        // 用户名或手机号都可登录：店长/店员/技师都用手机号，平台 admin 还能用 username
        var key = (req.Username ?? string.Empty).Trim();
        var query = _db.Users.Where(u =>
            (u.Username == key || (u.Phone != null && u.Phone == key)) && u.IsActive);
        if (!string.IsNullOrWhiteSpace(req.TenantCode))
        {
            query = query.Where(u => u.Tenant != null && u.Tenant.Name == req.TenantCode);
        }

        var user = await query.Include(u => u.Tenant).FirstOrDefaultAsync(ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            return Unauthorized(new { code = "InvalidCredentials", message = "用户名或密码错误" });
        }

        if (user.Role != UserRole.PlatformAdmin && user.Tenant != null)
        {
            if (user.Tenant.Status == TenantStatus.Disabled)
            {
                return Unauthorized(new { code = "TenantDisabled", message = "该按摩店已被停用" });
            }
        }

        var (accessToken, expiresAt) = _tokenService.CreateAccessToken(user);
        var refreshToken = _tokenService.CreateRefreshToken();

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} ({Username}) logged in", user.Id, user.Username);

        return Ok(new LoginResponse(
            accessToken,
            refreshToken,
            expiresAt,
            new UserInfo(user.Id, user.Username, user.RealName, user.Role.ToString(),
                         user.TenantId, user.StoreId, user.IsBlind)));
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<object> Me()
    {
        return Ok(new
        {
            userId = _tenantContext.UserId,
            tenantId = _tenantContext.TenantId,
            isPlatformAdmin = _tenantContext.IsPlatformAdmin
        });
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetProfile(CancellationToken ct)
    {
        if (_tenantContext.UserId is not long userId)
            return Unauthorized();

        _tenantContext.BypassTenantFilter();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return NotFound();

        return Ok(new UserProfileDto(user.Id, user.Username, user.RealName, user.Phone,
            user.Role.ToString(), user.TenantId, user.StoreId));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile(
        [FromBody] UpdateProfileRequest req,
        CancellationToken ct)
    {
        if (_tenantContext.UserId is not long userId)
            return Unauthorized();

        _tenantContext.BypassTenantFilter();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return NotFound();

        var newPhone = req.Phone?.Trim();
        if (!string.IsNullOrEmpty(newPhone))
        {
            if (newPhone.Length != 11 || !newPhone.All(char.IsDigit))
                return BadRequest(new { code = "InvalidPhone", message = "请输入 11 位手机号" });

            if (newPhone != user.Phone)
            {
                var taken = await _db.Users.AnyAsync(u => u.Id != userId && u.Phone == newPhone, ct);
                if (taken)
                    return Conflict(new { code = "PhoneTaken", message = "该手机号已被使用" });
            }
        }

        user.RealName = string.IsNullOrWhiteSpace(req.RealName) ? null : req.RealName.Trim();
        if (!string.IsNullOrEmpty(newPhone)) user.Phone = newPhone;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("User {UserId} updated profile (realName/phone)", user.Id);

        return Ok(new UserProfileDto(user.Id, user.Username, user.RealName, user.Phone,
            user.Role.ToString(), user.TenantId, user.StoreId));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest req,
        CancellationToken ct)
    {
        if (_tenantContext.UserId is not long userId)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(req.OldPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
            return BadRequest(new { code = "InvalidInput", message = "旧密码与新密码必填" });
        if (req.NewPassword.Length < 6)
            return BadRequest(new { code = "WeakPassword", message = "新密码至少 6 位" });
        if (req.OldPassword == req.NewPassword)
            return BadRequest(new { code = "SamePassword", message = "新密码不能与旧密码相同" });

        _tenantContext.BypassTenantFilter();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(req.OldPassword, user.PasswordHash))
            return BadRequest(new { code = "WrongPassword", message = "旧密码错误" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("User {UserId} changed password", user.Id);
        return NoContent();
    }
}
