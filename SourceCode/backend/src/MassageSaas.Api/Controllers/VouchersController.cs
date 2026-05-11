using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Vouchers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/vouchers")]
[Authorize(Policy = "ShopStaff")]
public class VouchersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<VouchersController> _logger;

    public VouchersController(ApplicationDbContext db, ITenantContext tenantContext, ILogger<VouchersController> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VoucherDto>>> List(
        [FromQuery] string? status = null,
        [FromQuery] string? keyword = null,
        CancellationToken ct = default)
    {
        var q = _db.Vouchers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<VoucherStatus>(status, true, out var s))
            q = q.Where(v => v.Status == s);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(v => v.Code.Contains(k) || v.Title.Contains(k));
        }
        var rows = await q.OrderByDescending(v => v.CreatedAt).Take(300).ToListAsync(ct);
        return Ok(rows.Select(MapDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<VoucherDto>> Create([FromBody] CreateVoucherRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<VoucherKind>(req.Kind, true, out var kind))
            return BadRequest(new { code = "InvalidKind", message = "券类型不合法" });
        if (string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { code = "InvalidInput", message = "券码与标题必填" });
        if (req.FaceValue < 0) return BadRequest(new { code = "InvalidFace", message = "面值不能为负" });

        var dup = await _db.Vouchers.AnyAsync(v => v.Code == req.Code, ct);
        if (dup) return Conflict(new { code = "DuplicateCode", message = "券码已存在" });

        var v = new Voucher
        {
            Kind = kind,
            Code = req.Code.Trim(),
            Title = req.Title.Trim(),
            FaceValue = req.FaceValue,
            MinOrderAmount = req.MinOrderAmount,
            DiscountPercent = req.DiscountPercent,
            ValidFrom = req.ValidFrom,
            ExpiresAt = req.ExpiresAt,
            Platform = req.Platform,
            Remark = req.Remark,
            Status = VoucherStatus.Active
        };
        _db.Vouchers.Add(v);
        await _db.SaveChangesAsync(ct);
        return Ok(MapDto(v));
    }

    /// <summary>
    /// 核销：把券挂到订单上，订单结账时按面值/折扣抵扣。订单需在 Pending/InProgress。
    /// </summary>
    [HttpPost("redeem")]
    public async Task<ActionResult<VoucherDto>> Redeem([FromBody] VoucherRedeemRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Code))
            return BadRequest(new { code = "InvalidCode", message = "请填写券码" });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var voucher = await _db.Vouchers.FirstOrDefaultAsync(x => x.Code == req.Code.Trim(), ct);
        if (voucher is null) return NotFound(new { code = "VoucherNotFound", message = "券不存在" });
        if (voucher.Status != VoucherStatus.Active)
            return Conflict(new { code = "VoucherInvalid", message = "券已使用/作废/过期" });
        var now = DateTime.UtcNow;
        if (voucher.ValidFrom.HasValue && now < voucher.ValidFrom.Value)
            return Conflict(new { code = "VoucherInvalid", message = "券尚未生效" });
        if (voucher.ExpiresAt.HasValue && now > voucher.ExpiresAt.Value)
        {
            voucher.Status = VoucherStatus.Expired;
            await _db.SaveChangesAsync(ct);
            return Conflict(new { code = "VoucherExpired", message = "券已过期" });
        }

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == req.OrderId, ct);
        if (order is null) return NotFound(new { code = "OrderNotFound", message = "订单不存在" });
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.InProgress)
            return Conflict(new { code = "OrderInvalid", message = "仅未结账订单可核销" });
        if (order.VoucherId.HasValue)
            return Conflict(new { code = "AlreadyRedeemed", message = "本单已挂券" });
        if (voucher.MinOrderAmount > 0 && order.Total < voucher.MinOrderAmount)
            return Conflict(new { code = "BelowMin", message = $"订单不足券要求最低 {voucher.MinOrderAmount} 元" });

        order.VoucherId = voucher.Id;
        voucher.Status = VoucherStatus.Redeemed;
        voucher.RedeemedAt = now;
        voucher.RedeemedOrderId = order.Id;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation("Voucher {Code} redeemed on order {OrderId}", voucher.Code, order.Id);
        return Ok(MapDto(voucher));
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var v = await _db.Vouchers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return NotFound();
        if (v.Status == VoucherStatus.Redeemed)
            return Conflict(new { code = "AlreadyRedeemed", message = "已核销券不可作废" });
        v.Status = VoucherStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static VoucherDto MapDto(Voucher v) => new(
        v.Id, v.Kind.ToString(), v.Code, v.Title,
        v.FaceValue, v.MinOrderAmount, v.DiscountPercent,
        v.ValidFrom, v.ExpiresAt, v.Status.ToString(),
        v.Platform, v.Remark,
        v.RedeemedAt, v.RedeemedOrderId, v.CreatedAt);
}
