using System.Security.Claims;
using MassageSaas.Application.Abstractions;
using MassageSaas.Infrastructure.Multitenancy;

namespace MassageSaas.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (tenantContext is TenantContext ctx && context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
            if (long.TryParse(tenantClaim, out var tenantId))
            {
                ctx.TenantId = tenantId;
            }

            var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
            ctx.IsPlatformAdmin = role == "PlatformAdmin";

            if (long.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            {
                ctx.UserId = userId;
            }

            if (ctx.IsPlatformAdmin)
            {
                ctx.BypassTenantFilter();
            }
        }

        await _next(context);
    }
}
