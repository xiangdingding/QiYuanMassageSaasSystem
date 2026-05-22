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
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        ApplicationDbContext db,
        ITenantContext tenantContext,
        IConfiguration configuration,
        ILogger<TenantsController> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _configuration = configuration;
        _logger = logger;
        _tenantContext.BypassTenantFilter();
    }

    /// <summary>取 Tenancy:DefaultTrialDays，缺省 30，限制在 1-365 之间。</summary>
    private int ResolveTrialDays(int? requested)
    {
        var defaultDays = _configuration.GetValue<int?>("Tenancy:DefaultTrialDays") ?? 30;
        var picked = requested ?? defaultDays;
        return Math.Clamp(picked, 1, 365);
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
        // 先按 EF 可翻译的字段投影到匿名类型，再在客户端把"到期天数"算出来——
        // Pomelo 翻译不了 (DateTime - DateTime).TotalDays → int? 的投影
        var raw = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((q.SafePage - 1) * q.SafePageSize)
            .Take(q.SafePageSize)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.ContactPhone,
                t.ContactName,
                t.Status,
                t.ExpireAt,
                t.CurrentPlanId,
                CurrentPlanName = t.CurrentPlan != null ? t.CurrentPlan.Name : null
            })
            .ToListAsync(ct);

        // 当前页租户的最新一笔订阅：开始时间 + 年限（年限按 EndAt-StartAt 天数 / 365 取整，
        // 所有续费/激活路径都按整年走）
        var tenantIds = raw.Select(t => t.Id).ToList();
        var latestSubs = (await _db.Subscriptions.AsNoTracking()
                .Where(s => s.TenantId != null && tenantIds.Contains(s.TenantId.Value))
                .Select(s => new { TenantId = s.TenantId!.Value, s.StartAt, s.EndAt, s.CreatedAt })
                .ToListAsync(ct))
            .GroupBy(s => s.TenantId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.CreatedAt).First());

        var items = raw.Select(t =>
        {
            DateTime? startAt = null;
            int? years = null;
            if (latestSubs.TryGetValue(t.Id, out var sub))
            {
                startAt = sub.StartAt;
                years = (int)Math.Round((sub.EndAt - sub.StartAt).TotalDays / 365.0);
                if (years < 1) years = 1;
            }
            return new TenantSummaryDto(
                t.Id,
                t.Name,
                t.ContactPhone,
                t.ContactName,
                t.Status.ToString(),
                t.ExpireAt,
                t.CurrentPlanId,
                t.CurrentPlanName,
                t.ExpireAt.HasValue ? (int?)(t.ExpireAt.Value - now).TotalDays : null,
                startAt,
                years);
        }).ToList();

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
        if (string.IsNullOrWhiteSpace(req.OwnerPhone) || string.IsNullOrWhiteSpace(req.OwnerPassword))
            return BadRequest(new { code = "InvalidInput", message = "店主手机号与密码必填" });
        if (req.OwnerPassword.Length < 6)
            return BadRequest(new { code = "WeakPassword", message = "密码至少 6 位" });

        var ownerPhone = req.OwnerPhone.Trim();
        var phoneTaken = await _db.Users.AnyAsync(u => u.Phone == ownerPhone, ct);
        if (phoneTaken)
            return Conflict(new { code = "PhoneTaken", message = "该手机号已被注册，请换一个" });

        var trialDays = ResolveTrialDays(req.TrialDays);
        var now = DateTime.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // 新建租户默认 N 天试用：Status=Trial、ExpireAt=now+TrialDays、无 Subscription 与 PaymentOrder。
        // 试用期内可正常使用；到期后中间件拦截写请求；激活时由 ActivateOffline 建 PaymentOrder + Subscription，
        // 此时金额才会入大盘营收。
        var tenant = new Tenant
        {
            Name = req.Name.Trim(),
            ContactPhone = req.ContactPhone.Trim(),
            ContactName = req.ContactName?.Trim(),
            Status = TenantStatus.Trial,
            CurrentPlanId = null,
            ExpireAt = now.AddDays(trialDays)
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

        // 店主登录标识 = 手机号：Username 也用手机号填，使 AuthController 的双匹配逻辑都能命中
        var owner = new User
        {
            TenantId = tenant.Id,
            StoreId = headquarters.Id,
            Username = ownerPhone,
            Phone = ownerPhone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.OwnerPassword),
            RealName = req.OwnerRealName?.Trim(),
            Role = UserRole.ShopOwner,
            IsActive = true
        };
        _db.Users.Add(owner);
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
        _logger.LogInformation(
            "Created tenant {TenantId} ({Name}) trial={TrialDays}d ownerPhone={Phone}",
            tenant.Id, tenant.Name, trialDays, ownerPhone);

        return CreatedAtAction(nameof(Get), new { id = tenant.Id },
            new TenantDetailDto(tenant.Id, tenant.Name, tenant.ContactPhone, tenant.ContactName,
                tenant.Status.ToString(), tenant.ExpireAt, tenant.CurrentPlanId,
                null, 1, 1, tenant.CreatedAt));
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

    /// <summary>
    /// 删除未激活的租户：仅当该租户从未有过任何 Subscription 记录时允许，
    /// 级联清掉自动建立的总店与店主账号。已激活/已欠费/已停用都不能走这里——
    /// 那些都有真实业务数据，应走"停用"软删而非物理删除。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null) return NotFound();

        var hasSubscription = await _db.Subscriptions.AnyAsync(s => s.TenantId == id, ct);
        if (hasSubscription)
        {
            return BadRequest(new
            {
                code = "TenantAlreadyActivated",
                message = "该租户已有订阅记录，不可删除；请使用「停用」操作"
            });
        }

        var hasPayment = await _db.PaymentOrders.AnyAsync(p => p.TenantId == id, ct);
        if (hasPayment)
        {
            return BadRequest(new
            {
                code = "TenantHasPaymentHistory",
                message = "该租户存在历史支付订单，不可删除"
            });
        }

        var stores = await _db.Stores.Where(s => s.TenantId == id).ToListAsync(ct);
        var users = await _db.Users.Where(u => u.TenantId == id).ToListAsync(ct);

        _db.Stores.RemoveRange(stores);
        _db.Users.RemoveRange(users);
        _db.Tenants.Remove(tenant);

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Deleted unactivated tenant {TenantId} ({Name}); cascaded {StoreCount} stores, {UserCount} users",
            id, tenant.Name, stores.Count, users.Count);

        return NoContent();
    }

    /// <summary>
    /// 按摩店自助注册：公开匿名端点，注册后获得固定 30 天试用（Tenancy:DefaultTrialDays）。
    /// 注册者用返回的 ownerUsername + 自己设置的密码登录 shop-admin。
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterTenantResponse>> Register(
        [FromBody] RegisterTenantRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.ContactPhone))
            return BadRequest(new { code = "InvalidInput", message = "店铺名与联系电话必填" });
        if (string.IsNullOrWhiteSpace(req.OwnerPhone) || string.IsNullOrWhiteSpace(req.OwnerPassword))
            return BadRequest(new { code = "InvalidInput", message = "登录手机号与密码必填" });
        if (req.OwnerPassword.Length < 6)
            return BadRequest(new { code = "WeakPassword", message = "密码至少 6 位" });

        var ownerPhone = req.OwnerPhone.Trim();
        // 手机号即登录标识：全局唯一，提前查重给清晰错误（同时也命中 Username 列因为我们存的一样）
        var phoneTaken = await _db.Users.AnyAsync(u => u.Phone == ownerPhone || u.Username == ownerPhone, ct);
        if (phoneTaken)
            return Conflict(new { code = "PhoneTaken", message = "该手机号已被注册，请直接登录或换一个" });

        var trialDays = ResolveTrialDays(null);
        var now = DateTime.UtcNow;
        var expireAt = now.AddDays(trialDays);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var tenant = new Tenant
        {
            Name = req.Name.Trim(),
            ContactPhone = req.ContactPhone.Trim(),
            ContactName = req.ContactName?.Trim(),
            Status = TenantStatus.Trial,
            CurrentPlanId = null,
            ExpireAt = expireAt
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        var headquarters = new Store
        {
            TenantId = tenant.Id,
            ParentStoreId = null,
            Name = $"{tenant.Name}总店",
            IsActive = true
        };
        _db.Stores.Add(headquarters);
        await _db.SaveChangesAsync(ct);

        var owner = new User
        {
            TenantId = tenant.Id,
            StoreId = headquarters.Id,
            Username = ownerPhone,
            Phone = ownerPhone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.OwnerPassword),
            RealName = req.OwnerRealName?.Trim(),
            Role = UserRole.ShopOwner,
            IsActive = true
        };
        _db.Users.Add(owner);
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
        _logger.LogInformation(
            "Self-registered tenant {TenantId} ({Name}) trial={TrialDays}d ownerPhone={Phone}",
            tenant.Id, tenant.Name, trialDays, ownerPhone);

        return Ok(new RegisterTenantResponse(tenant.Id, tenant.Name, ownerPhone, expireAt, trialDays));
    }
}
