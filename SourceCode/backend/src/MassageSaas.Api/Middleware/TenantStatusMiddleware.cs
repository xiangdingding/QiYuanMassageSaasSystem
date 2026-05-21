using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Middleware;

public class TenantStatusMiddleware
{
    private static readonly HashSet<string> WriteMethods = new(StringComparer.OrdinalIgnoreCase)
        { "POST", "PUT", "PATCH", "DELETE" };

    private static readonly string[] Whitelist =
    {
        "/api/auth/",
        "/api/callback/",
        "/api/subscriptions/pay",
        "/api/subscriptions/activate-offline",
        "/api/tenants/register",
        "/health"
    };

    private readonly RequestDelegate _next;

    public TenantStatusMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ApplicationDbContext db)
    {
        if (!WriteMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (Whitelist.Any(w => path.StartsWith(w, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        if (tenantContext.TenantId == null || tenantContext.IsPlatformAdmin)
        {
            await _next(context);
            return;
        }

        tenantContext.BypassTenantFilter();
        var tenant = await db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantContext.TenantId)
            .Select(t => new { t.Status, t.ExpireAt })
            .FirstOrDefaultAsync();

        if (tenant is null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { code = "TenantNotFound", message = "租户不存在" });
            return;
        }

        // Trial 与 Active 同样允许写——是否到期完全由 ExpireAt 判断
        var expired = tenant.Status == TenantStatus.Expired
            || tenant.Status == TenantStatus.Disabled
            || (tenant.ExpireAt.HasValue && tenant.ExpireAt.Value < DateTime.UtcNow);

        if (expired)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                code = "TenantExpired",
                message = "订阅已到期或账号被停用，请续费后恢复写操作",
                expireAt = tenant.ExpireAt
            });
            return;
        }

        await _next(context);
    }
}
