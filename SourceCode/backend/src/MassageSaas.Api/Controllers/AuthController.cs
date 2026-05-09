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

        var query = _db.Users.Where(u => u.Username == req.Username && u.IsActive);
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
}
