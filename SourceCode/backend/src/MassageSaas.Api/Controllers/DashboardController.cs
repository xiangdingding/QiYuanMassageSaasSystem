using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "PlatformAdmin")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public DashboardController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
        _tenantContext.BypassTenantFilter();
    }

    [HttpGet("platform")]
    public async Task<ActionResult<PlatformDashboardDto>> Platform(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var in30 = now.AddDays(30);
        var in7 = now.AddDays(7);
        var since30 = now.AddDays(-30);
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var tenants = _db.Tenants.AsNoTracking();

        var total = await tenants.CountAsync(ct);
        var active = await tenants.CountAsync(t => t.Status == TenantStatus.Active, ct);
        var expired = await tenants.CountAsync(t => t.Status == TenantStatus.Expired, ct);
        var disabled = await tenants.CountAsync(t => t.Status == TenantStatus.Disabled, ct);
        var expIn30 = await tenants.CountAsync(t =>
            t.Status == TenantStatus.Active && t.ExpireAt != null
            && t.ExpireAt > now && t.ExpireAt <= in30, ct);
        var expIn7 = await tenants.CountAsync(t =>
            t.Status == TenantStatus.Active && t.ExpireAt != null
            && t.ExpireAt > now && t.ExpireAt <= in7, ct);

        var paidOrders = _db.PaymentOrders.AsNoTracking().Where(o => o.Status == PaymentStatus.Paid);
        var revenue30 = await paidOrders
            .Where(o => o.PaidAt >= since30)
            .SumAsync(o => (decimal?)o.Amount, ct) ?? 0m;
        var revenueYtd = await paidOrders
            .Where(o => o.PaidAt >= yearStart)
            .SumAsync(o => (decimal?)o.Amount, ct) ?? 0m;
        var paidOrderCount30 = await paidOrders.CountAsync(o => o.PaidAt >= since30, ct);

        var recent = await _db.Subscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Include(s => s.Tenant)
            .Include(s => s.PaymentOrder)
            .OrderByDescending(s => s.CreatedAt)
            .Take(10)
            .Select(s => new RecentSubscriptionDto(
                s.Id,
                s.TenantId!.Value,
                s.Tenant.Name,
                s.Plan.Name,
                s.PaymentOrder != null ? s.PaymentOrder.Amount : 0m,
                s.Source.ToString(),
                s.CreatedAt))
            .ToListAsync(ct);

        return Ok(new PlatformDashboardDto(
            total, active, expired, disabled,
            expIn30, expIn7, revenue30, revenueYtd, paidOrderCount30, recent));
    }
}
