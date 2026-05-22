using MassageSaas.Application.Abstractions;
using MassageSaas.Application.Subscriptions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        ApplicationDbContext db,
        ITenantContext tenantContext,
        ILogger<SubscriptionsController> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpPost("pay")]
    [Authorize]
    public async Task<ActionResult<SubscriptionPaymentDto>> CreatePayment(
        [FromBody] CreateSubscriptionPaymentRequest req,
        CancellationToken ct)
    {
        if (req.Years < 1)
            return BadRequest(new { code = "InvalidInput", message = "购买年限必须 ≥ 1" });

        if (!Enum.TryParse<PaymentChannel>(req.Channel, true, out var channel) || channel == PaymentChannel.Offline)
            return BadRequest(new { code = "InvalidChannel", message = "支付渠道必须是 Wechat 或 Alipay" });

        if (!_tenantContext.IsPlatformAdmin && _tenantContext.TenantId != req.TenantId)
            return Forbid();

        _tenantContext.BypassTenantFilter();

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == req.TenantId, ct);
        if (tenant is null) return NotFound(new { code = "TenantNotFound", message = "租户不存在" });

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == req.PlanId && p.IsActive, ct);
        if (plan is null) return BadRequest(new { code = "PlanNotFound", message = "套餐不存在或已停用" });

        var amount = plan.AnnualPrice * req.Years;
        var orderNo = $"SUB{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        var order = new PaymentOrder
        {
            TenantId = tenant.Id,
            OrderNo = orderNo,
            Amount = amount,
            Channel = channel,
            Status = PaymentStatus.Pending,
            PlanId = plan.Id,
            Years = req.Years
        };
        _db.PaymentOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        // 真实支付集成在 P2 完成；此处返回占位 URL，前端可据此渲染二维码
        var payUrl = $"weixin://wxpay/bizpayurl?pr={orderNo}";

        return Ok(new SubscriptionPaymentDto(
            order.Id, order.OrderNo, order.Amount, channel.ToString(),
            order.Status.ToString(), payUrl, order.CreatedAt));
    }

    [HttpPost("activate-offline")]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<ActionResult<SubscriptionDto>> ActivateOffline(
        [FromBody] OfflineActivateRequest req,
        CancellationToken ct)
    {
        if (req.Years < 1) return BadRequest(new { code = "InvalidInput", message = "年限必须 ≥ 1" });
        _tenantContext.BypassTenantFilter();

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == req.TenantId, ct);
        if (tenant is null) return NotFound(new { code = "TenantNotFound", message = "租户不存在" });

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == req.PlanId, ct);
        if (plan is null) return BadRequest(new { code = "PlanNotFound", message = "套餐不存在" });

        var now = DateTime.UtcNow;
        var (startAt, endAt) = SubscriptionExtension.Renew(tenant.ExpireAt, req.Years, now);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var paymentOrder = new PaymentOrder
        {
            TenantId = tenant.Id,
            OrderNo = $"OFL{now:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}",
            Amount = req.AmountReceived,
            Channel = PaymentChannel.Offline,
            Status = PaymentStatus.Paid,
            PaidAt = now,
            PlanId = plan.Id,
            Years = req.Years
        };
        _db.PaymentOrders.Add(paymentOrder);
        await _db.SaveChangesAsync(ct);

        var subscription = new Subscription
        {
            TenantId = tenant.Id,
            PlanId = plan.Id,
            StartAt = startAt,
            EndAt = endAt,
            Source = SubscriptionSource.OfflineManual,
            PaymentOrderId = paymentOrder.Id,
            Remark = req.Remark
        };
        _db.Subscriptions.Add(subscription);

        tenant.CurrentPlanId = plan.Id;
        tenant.ExpireAt = endAt;
        tenant.Status = TenantStatus.Active;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation(
            "Offline activated tenant {TenantId} plan {PlanId} {Years}y -> {EndAt:O}",
            tenant.Id, plan.Id, req.Years, endAt);

        return Ok(new SubscriptionDto(
            subscription.Id, tenant.Id, plan.Id, plan.Name,
            subscription.StartAt, subscription.EndAt,
            subscription.Source.ToString(), subscription.Remark, subscription.CreatedAt));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<TenantSubscriptionStatusDto>> GetMyStatus(CancellationToken ct)
    {
        if (_tenantContext.TenantId is not long tenantId)
            return BadRequest(new { code = "NoTenant", message = "当前账号未绑定租户" });

        _tenantContext.BypassTenantFilter();
        var tenant = await _db.Tenants
            .Include(t => t.CurrentPlan)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null) return NotFound();

        var days = tenant.ExpireAt.HasValue
            ? (int?)(tenant.ExpireAt.Value - DateTime.UtcNow).TotalDays
            : null;

        return Ok(new TenantSubscriptionStatusDto(
            tenant.Id, tenant.Status.ToString(), tenant.ExpireAt, days,
            tenant.CurrentPlanId, tenant.CurrentPlan?.Name));
    }

    /// <summary>
    /// 查询支付单状态，供前端在拉起支付后轮询。
    /// 当前租户只能查自己；平台管理员可任意查。
    /// </summary>
    [HttpGet("pay/{orderNo}")]
    [Authorize]
    public async Task<ActionResult<SubscriptionPaymentDto>> GetPaymentStatus(
        string orderNo,
        CancellationToken ct)
    {
        _tenantContext.BypassTenantFilter();
        var order = await _db.PaymentOrders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderNo == orderNo, ct);
        if (order is null) return NotFound();

        if (!_tenantContext.IsPlatformAdmin && order.TenantId != _tenantContext.TenantId)
            return Forbid();

        return Ok(new SubscriptionPaymentDto(
            order.Id, order.OrderNo, order.Amount, order.Channel.ToString(),
            order.Status.ToString(), null, order.CreatedAt));
    }

    [HttpGet("history")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<SubscriptionDto>>> History(
        [FromQuery] long? tenantId,
        CancellationToken ct = default)
    {
        long targetTenantId;
        if (_tenantContext.IsPlatformAdmin)
        {
            if (tenantId is null) return BadRequest(new { code = "InvalidInput", message = "平台端需指定 tenantId" });
            targetTenantId = tenantId.Value;
            _tenantContext.BypassTenantFilter();
        }
        else
        {
            if (_tenantContext.TenantId is not long tid) return Forbid();
            if (tenantId.HasValue && tenantId.Value != tid) return Forbid();
            targetTenantId = tid;
        }

        var data = await _db.Subscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Where(s => s.TenantId == targetTenantId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SubscriptionDto(
                s.Id, s.TenantId!.Value, s.PlanId, s.Plan.Name,
                s.StartAt, s.EndAt, s.Source.ToString(), s.Remark, s.CreatedAt))
            .ToListAsync(ct);

        return Ok(data);
    }
}
