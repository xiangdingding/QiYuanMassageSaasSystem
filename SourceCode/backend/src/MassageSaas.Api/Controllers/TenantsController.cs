using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Common;
using MassageSaas.Shared.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Policy = "PlatformAdmin")]
public class TenantsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(ApplicationDbContext db, ITenantContext tenantContext, ILogger<TenantsController> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
        _tenantContext.BypassTenantFilter();
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TenantSummaryDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var q = new PageQuery(page, pageSize, keyword);
        var query = _db.Tenants.AsNoTracking().Include(t => t.CurrentPlan).AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(t => t.Name.Contains(k) || t.ContactPhone.Contains(k));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TenantStatus>(status, true, out var s))
        {
            query = query.Where(t => t.Status == s);
        }

        var total = await query.CountAsync(ct);
        var now = DateTime.UtcNow;
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((q.SafePage - 1) * q.SafePageSize)
            .Take(q.SafePageSize)
            .Select(t => new TenantSummaryDto(
                t.Id,
                t.Name,
                t.ContactPhone,
                t.ContactName,
                t.Status.ToString(),
                t.ExpireAt,
                t.CurrentPlanId,
                t.CurrentPlan != null ? t.CurrentPlan.Name : null,
                t.ExpireAt.HasValue ? (int?)(t.ExpireAt.Value - now).TotalDays : null))
            .ToListAsync(ct);

        return Ok(new PagedResult<TenantSummaryDto>(items, total, q.SafePage, q.SafePageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<TenantDetailDto>> Get(long id, CancellationToken ct)
    {
        var t = await _db.Tenants.AsNoTracking()
            .Include(x => x.CurrentPlan)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();

        var storeCount = await _db.Stores.CountAsync(s => s.TenantId == id, ct);
        var userCount = await _db.Users.CountAsync(u => u.TenantId == id, ct);

        return Ok(new TenantDetailDto(
            t.Id, t.Name, t.ContactPhone, t.ContactName,
            t.Status.ToString(), t.ExpireAt, t.CurrentPlanId,
            t.CurrentPlan?.Name, storeCount, userCount, t.CreatedAt));
    }

    /// <summary>单租户运营概览：门店/员工/会员规模、近 7/30 天营业额、技师营收榜。</summary>
    [HttpGet("{id:long}/overview")]
    public async Task<ActionResult<TenantOverviewDto>> Overview(long id, CancellationToken ct)
    {
        var t = await _db.Tenants.AsNoTracking()
            .Include(x => x.CurrentPlan)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();

        var now = DateTime.UtcNow;
        var since7 = now.AddDays(-7);
        var since30 = now.AddDays(-30);

        var storeCount = await _db.Stores.CountAsync(s => s.TenantId == id, ct);
        var activeStoreCount = await _db.Stores.CountAsync(s => s.TenantId == id && s.IsActive, ct);
        var staffCount = await _db.Users.CountAsync(u => u.TenantId == id, ct);
        var technicianCount = await _db.Users.CountAsync(
            u => u.TenantId == id && u.Role == UserRole.Technician && u.IsActive, ct);
        var memberCount = await _db.Members.CountAsync(m => m.TenantId == id, ct);

        // 营业额：已完成订单的实收额（不含小费），按完成时间归集
        var completed30 = _db.Orders.AsNoTracking()
            .Where(o => o.TenantId == id && o.Status == OrderStatus.Completed
                        && o.CompletedAt != null && o.CompletedAt >= since30);
        var revenue30 = await completed30.SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;
        var revenue7 = await completed30
            .Where(o => o.CompletedAt >= since7)
            .SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;
        var orderCount30 = await completed30.CountAsync(ct);

        var topRaw = await _db.OrderItems.AsNoTracking()
            .Where(i => i.TenantId == id && i.Order.Status == OrderStatus.Completed
                        && i.Order.CompletedAt != null && i.Order.CompletedAt >= since30)
            .GroupBy(i => i.TechnicianId)
            .Select(g => new { TechnicianId = g.Key, Rounds = g.Count(), Revenue = g.Sum(x => x.ItemTotal) })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToListAsync(ct);

        var techIds = topRaw.Select(x => x.TechnicianId).ToList();
        var techNames = await _db.Users.AsNoTracking()
            .Where(u => techIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.RealName ?? u.Username, ct);

        var topTechnicians = topRaw
            .Select(x => new TenantTopTechnicianDto(
                x.TechnicianId,
                techNames.TryGetValue(x.TechnicianId, out var n) ? n : $"#{x.TechnicianId}",
                x.Rounds, x.Revenue))
            .ToList();

        return Ok(new TenantOverviewDto(
            t.Id, t.Name, t.Status.ToString(), t.ExpireAt,
            t.ExpireAt.HasValue ? (int?)(t.ExpireAt.Value - now).TotalDays : null,
            t.CurrentPlan?.Name,
            storeCount, activeStoreCount, staffCount, technicianCount, memberCount,
            revenue7, revenue30, orderCount30, topTechnicians));
    }

    [HttpPost]
    public async Task<ActionResult<TenantDetailDto>> Create([FromBody] CreateTenantRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.ContactPhone))
            return BadRequest(new { code = "InvalidInput", message = "店铺名与联系电话必填" });
        if (string.IsNullOrWhiteSpace(req.OwnerUsername) || string.IsNullOrWhiteSpace(req.OwnerPassword))
            return BadRequest(new { code = "InvalidInput", message = "店主账号与密码必填" });
        if (req.OwnerPassword.Length < 6)
            return BadRequest(new { code = "WeakPassword", message = "密码至少 6 位" });

        Plan? plan = null;
        if (req.InitialPlanId.HasValue)
        {
            plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == req.InitialPlanId.Value, ct);
            if (plan is null) return BadRequest(new { code = "PlanNotFound", message = "套餐不存在" });
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var tenant = new Tenant
        {
            Name = req.Name.Trim(),
            ContactPhone = req.ContactPhone.Trim(),
            ContactName = req.ContactName?.Trim(),
            Status = plan != null ? TenantStatus.Active : TenantStatus.Expired,
            CurrentPlanId = plan?.Id,
            ExpireAt = plan != null ? DateTime.UtcNow.AddYears(1) : null
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        var headquarters = new Store
        {
            TenantId = tenant.Id,
            ParentStoreId = null,
            Name = string.IsNullOrWhiteSpace(req.HeadquartersName) ? $"{tenant.Name}总店" : req.HeadquartersName.Trim(),
            IsActive = true
        };
        _db.Stores.Add(headquarters);
        await _db.SaveChangesAsync(ct);

        var owner = new User
        {
            TenantId = tenant.Id,
            StoreId = headquarters.Id,
            Username = req.OwnerUsername.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.OwnerPassword),
            RealName = req.OwnerRealName?.Trim(),
            Role = UserRole.ShopOwner,
            IsActive = true
        };
        _db.Users.Add(owner);
        await _db.SaveChangesAsync(ct);

        if (plan != null)
        {
            var sub = new Subscription
            {
                TenantId = tenant.Id,
                PlanId = plan.Id,
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddYears(1),
                Source = SubscriptionSource.OfflineManual,
                Remark = "新建租户初始订阅"
            };
            _db.Subscriptions.Add(sub);
            await _db.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);
        _logger.LogInformation("Created tenant {TenantId} ({Name}) with owner {Username}", tenant.Id, tenant.Name, owner.Username);

        return CreatedAtAction(nameof(Get), new { id = tenant.Id },
            new TenantDetailDto(tenant.Id, tenant.Name, tenant.ContactPhone, tenant.ContactName,
                tenant.Status.ToString(), tenant.ExpireAt, tenant.CurrentPlanId,
                plan?.Name, 1, 1, tenant.CreatedAt));
    }

    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateTenantStatusRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<TenantStatus>(req.Status, true, out var newStatus))
            return BadRequest(new { code = "InvalidStatus", message = "状态值不合法" });

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null) return NotFound();

        tenant.Status = newStatus;
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Tenant {TenantId} status changed to {Status}", id, newStatus);
        return NoContent();
    }
}
