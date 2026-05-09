using System.Text.Json;
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

/// <summary>
/// 第三方支付回调入口。微信支付 V3 和支付宝异步通知都落在此处。
/// 真实签名校验与解密放在 CallbackVerifier（占位）里，回调体经过解密后归一为
/// PaymentCallbackPayload，然后由 ProcessAsync 走相同的幂等业务流程。
/// </summary>
[ApiController]
[Route("api/callback")]
[AllowAnonymous]
public class CallbackController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CallbackController> _logger;

    public CallbackController(
        ApplicationDbContext db,
        ITenantContext tenantContext,
        ILogger<CallbackController> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpPost("wechat")]
    public async Task<IActionResult> Wechat([FromBody] JsonElement body, CancellationToken ct)
    {
        // P2 占位：真实环境此处需要 wechatpay-apiv3 SDK 解密 resource.ciphertext。
        // 当前接受归一化字段直接驱动业务，方便联调。
        var payload = TryParseAsCanonical(body, PaymentChannel.Wechat);
        if (payload is null)
            return Ok(new { code = "FAIL", message = "回调数据无法解析" }); // 微信约定 200 + FAIL 即重试

        var ack = await ProcessAsync(payload, ct);
        return ack.Ok
            ? Ok(new { code = "SUCCESS", message = "OK" })
            : Ok(new { code = "FAIL", message = ack.Message ?? "处理失败" });
    }

    [HttpPost("alipay")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Alipay([FromForm] IFormCollection form, CancellationToken ct)
    {
        // P2 占位：真实环境需 alipay-easysdk 验签。
        var outTradeNo = form["out_trade_no"].FirstOrDefault();
        var tradeNo = form["trade_no"].FirstOrDefault();
        var totalAmount = form["total_amount"].FirstOrDefault();
        var tradeStatus = form["trade_status"].FirstOrDefault();

        if (string.IsNullOrEmpty(outTradeNo) || string.IsNullOrEmpty(tradeNo))
            return Content("failure");

        if (!decimal.TryParse(totalAmount, out var amount)) amount = 0;
        var success = tradeStatus is "TRADE_SUCCESS" or "TRADE_FINISHED";

        var rawJson = JsonSerializer.Serialize(form.ToDictionary(kv => kv.Key, kv => kv.Value.ToString()));
        var payload = new PaymentCallbackPayload(outTradeNo, tradeNo, amount, success, "Alipay", rawJson);

        var ack = await ProcessAsync(payload, ct);
        return Content(ack.Ok ? "success" : "failure");
    }

    /// <summary>
    /// 联调/线下使用：直接喂归一化结构，避开真实通道的签名细节。
    /// 仅在 Development 环境暴露。
    /// </summary>
    [HttpPost("simulate")]
    public async Task<ActionResult<CallbackAck>> Simulate(
        [FromBody] PaymentCallbackPayload payload,
        [FromServices] IWebHostEnvironment env,
        CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();
        var ack = await ProcessAsync(payload, ct);
        return Ok(ack);
    }

    /// <summary>
    /// 幂等业务流程：
    /// 1) 按 OrderNo 锁定 PaymentOrder。
    /// 2) 已支付直接返回成功（重放安全）。
    /// 3) 状态置为 Paid + 写第三方流水（third_trx 为唯一索引）。
    /// 4) 创建 Subscription 记录、续期 Tenant.ExpireAt、置 Active。
    /// </summary>
    private async Task<CallbackAck> ProcessAsync(PaymentCallbackPayload payload, CancellationToken ct)
    {
        _tenantContext.BypassTenantFilter();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var order = await _db.PaymentOrders
            .FirstOrDefaultAsync(p => p.OrderNo == payload.OutTradeNo, ct);
        if (order is null)
        {
            _logger.LogWarning("Callback for unknown order {OrderNo}", payload.OutTradeNo);
            return new CallbackAck(false, "OrderNotFound", "支付单不存在");
        }

        if (order.Status == PaymentStatus.Paid)
        {
            _logger.LogInformation("Callback replay for paid order {OrderNo}", order.OrderNo);
            await tx.CommitAsync(ct);
            return new CallbackAck(true, null, "已支付");
        }

        if (!payload.Success)
        {
            order.Status = PaymentStatus.Failed;
            order.RawCallbackPayload = payload.RawJson;
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return new CallbackAck(true, null, "已记录失败回调");
        }

        if (Math.Abs(order.Amount - payload.AmountYuan) > 0.01m)
        {
            _logger.LogError(
                "Callback amount mismatch order={OrderNo} expected={Expected} got={Got}",
                order.OrderNo, order.Amount, payload.AmountYuan);
            return new CallbackAck(false, "AmountMismatch", "金额校验失败");
        }

        var now = DateTime.UtcNow;
        order.Status = PaymentStatus.Paid;
        order.PaidAt = now;
        order.ThirdPartyTransactionNo = payload.ThirdTradeNo;
        order.RawCallbackPayload = payload.RawJson;

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == order.TenantId, ct);
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == order.PlanId, ct);
        if (tenant is null || plan is null)
        {
            _logger.LogError("Callback orphan: tenant={TenantId} plan={PlanId}", order.TenantId, order.PlanId);
            return new CallbackAck(false, "Orphan", "租户或套餐不存在");
        }

        var (startAt, endAt) = SubscriptionExtension.Renew(tenant.ExpireAt, order.Years, now);
        var subscription = new Subscription
        {
            TenantId = tenant.Id,
            PlanId = plan.Id,
            StartAt = startAt,
            EndAt = endAt,
            Source = SubscriptionSource.OnlinePayment,
            PaymentOrderId = order.Id
        };
        _db.Subscriptions.Add(subscription);

        tenant.CurrentPlanId = plan.Id;
        tenant.ExpireAt = endAt;
        tenant.Status = TenantStatus.Active;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation(
            "Tenant {TenantId} renewed via {Channel}, +{Years}y -> {EndAt:O}",
            tenant.Id, payload.Channel, order.Years, endAt);

        return new CallbackAck(true, null, "OK");
    }

    private static PaymentCallbackPayload? TryParseAsCanonical(JsonElement body, PaymentChannel channel)
    {
        // 真实环境：从 resource.ciphertext 解密为业务体；此处兼容直传归一字段。
        try
        {
            string? outTradeNo = null;
            string? thirdTradeNo = null;
            decimal amount = 0;
            var success = false;

            if (body.TryGetProperty("out_trade_no", out var ot)) outTradeNo = ot.GetString();
            if (body.TryGetProperty("transaction_id", out var ti)) thirdTradeNo = ti.GetString();
            if (body.TryGetProperty("trade_state", out var ts)) success = ts.GetString() == "SUCCESS";
            if (body.TryGetProperty("amount", out var am) && am.TryGetProperty("total", out var totalCents))
            {
                amount = totalCents.GetInt32() / 100m;
            }

            if (string.IsNullOrEmpty(outTradeNo) || string.IsNullOrEmpty(thirdTradeNo)) return null;
            return new PaymentCallbackPayload(outTradeNo, thirdTradeNo, amount, success,
                channel.ToString(), body.GetRawText());
        }
        catch
        {
            return null;
        }
    }
}
