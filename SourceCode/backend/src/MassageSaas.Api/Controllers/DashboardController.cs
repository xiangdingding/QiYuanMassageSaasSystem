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

    /// <summary>平台营收报表：近 N 个月订阅付费的月度趋势、按套餐/渠道拆分、新签 vs 续费。</summary>
    [HttpGet("platform/revenue")]
    public async Task<ActionResult<PlatformRevenueDto>> Revenue(
        [FromQuery] int months = 12, CancellationToken ct = default)
    {
        months = Math.Clamp(months, 1, 36);
        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var windowStart = currentMonthStart.AddMonths(-(months - 1));

        var orders = await _db.PaymentOrders.AsNoTracking()
            .Where(o => o.Status == PaymentStatus.Paid && o.PaidAt != null && o.PaidAt >= windowStart)
            .Select(o => new
            {
                o.Id,
                PaidAt = o.PaidAt!.Value,
                o.Amount,
                PlanName = o.Plan.Name,
                o.Channel
            })
            .ToListAsync(ct);

        // 每个租户最早一笔已支付订单 = 新签，其余 = 续费
        var firstOrderIds = (await _db.PaymentOrders.AsNoTracking()
            .Where(o => o.Status == PaymentStatus.Paid)
            .GroupBy(o => o.TenantId)
            .Select(g => g.Min(o => o.Id))
            .ToListAsync(ct)).ToHashSet();

        var monthly = new List<RevenueMonthDto>(months);
        for (var i = 0; i < months; i++)
        {
            var m = windowStart.AddMonths(i);
            var inMonth = orders.Where(o => o.PaidAt.Year == m.Year && o.PaidAt.Month == m.Month).ToList();
            monthly.Add(new RevenueMonthDto(m.Year, m.Month, inMonth.Sum(x => x.Amount), inMonth.Count));
        }

        var byPlan = orders
            .GroupBy(o => o.PlanName)
            .Select(g => new RevenueBreakdownDto(g.Key, g.Sum(x => x.Amount), g.Count()))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var byChannel = orders
            .GroupBy(o => o.Channel.ToString())
            .Select(g => new RevenueBreakdownDto(g.Key, g.Sum(x => x.Amount), g.Count()))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var total = orders.Sum(o => o.Amount);
        var newAmount = orders.Where(o => firstOrderIds.Contains(o.Id)).Sum(o => o.Amount);

        return Ok(new PlatformRevenueDto(
            months, total, orders.Count,
            newAmount, total - newAmount,
            monthly, byPlan, byChannel));
    }
}
