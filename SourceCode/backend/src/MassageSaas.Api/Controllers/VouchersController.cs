using System.Text;
using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Common;
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
    public async Task<ActionResult<PagedResult<VoucherDto>>> List(
        [FromQuery] string? status = null,
        [FromQuery] string? kind = null,
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var q = _db.Vouchers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<VoucherStatus>(status, true, out var s))
            q = q.Where(v => v.Status == s);
        if (!string.IsNullOrWhiteSpace(kind) && Enum.TryParse<VoucherKind>(kind, true, out var k0))
            q = q.Where(v => v.Kind == k0);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            // 一个输入框覆盖券码 / 标题 / 来源平台
            q = q.Where(v => v.Code.Contains(k) || v.Title.Contains(k)
                || (v.Platform != null && v.Platform.Contains(k)));
        }
        var pq = new PageQuery(page, pageSize, null);
        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(v => v.CreatedAt)
            .Skip((pq.SafePage - 1) * pq.SafePageSize)
            .Take(pq.SafePageSize)
            .ToListAsync(ct);
        return Ok(new PagedResult<VoucherDto>(rows.Select(MapDto).ToList(), total, pq.SafePage, pq.SafePageSize));
    }

    // 无歧义大写字母数字（去 0/O/1/I/L），与 BS/CS 单建对话框的生成器一致
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private static readonly Random Rng = new();

    /// <summary>校验 FaceValue/DiscountPercent 互斥 + 折扣率范围。返回 null 表示通过，否则返回拒绝原因。</summary>
    private static (string? Code, string? Message) ValidateDiscount(decimal faceValue, decimal? discountPercent)
    {
        if (faceValue < 0) return ("InvalidFace", "面值不能为负");
        var hasFace = faceValue > 0;
        var hasPercent = discountPercent.HasValue && discountPercent.Value > 0;
        if (hasFace && hasPercent) return ("InvalidDiscount", "满减面值与折扣率只能填一项");
        if (!hasFace && !hasPercent) return ("InvalidDiscount", "请填写满减面值或折扣率（0-1 之间）");
        if (hasPercent && (discountPercent!.Value <= 0 || discountPercent.Value >= 1))
            return ("InvalidDiscount", "折扣率需在 0-1 之间（如 0.9 表示 9 折）");
        return (null, null);
    }

    private static string GenerateCode(VoucherKind kind)
    {
        var prefix = kind == VoucherKind.GroupBuy ? "GB" : "SC";
        var sb = new StringBuilder(prefix.Length + 1 + 4 + 1 + 4);
        sb.Append(prefix).Append('-');
        for (var i = 0; i < 4; i++) sb.Append(CodeAlphabet[Rng.Next(CodeAlphabet.Length)]);
        sb.Append('-');
        for (var i = 0; i < 4; i++) sb.Append(CodeAlphabet[Rng.Next(CodeAlphabet.Length)]);
        return sb.ToString();
    }

    [HttpPost]
    public async Task<ActionResult<VoucherDto>> Create([FromBody] CreateVoucherRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<VoucherKind>(req.Kind, true, out var kind))
            return BadRequest(new { code = "InvalidKind", message = "券类型不合法" });
        if (string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { code = "InvalidInput", message = "券码与标题必填" });

        var (errCode, errMsg) = ValidateDiscount(req.FaceValue, req.DiscountPercent);
        if (errCode is not null) return BadRequest(new { code = errCode, message = errMsg });

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
    /// 批量生成同规格券：服务端按 SC-XXXX-XXXX / GB-XXXX-XXXX 规则随机生成 N 个无歧义码，
    /// 在同租户内去重（与已存在的 vouchers.Code 不冲突），一次性入库。
    /// 用途：店家集中印 100 张面值 100 元的团购券给抖音/美团做活动。
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<BatchCreateVoucherResponse>> BatchCreate(
        [FromBody] BatchCreateVoucherRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<VoucherKind>(req.Kind, true, out var kind))
            return BadRequest(new { code = "InvalidKind", message = "券类型不合法" });
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { code = "InvalidInput", message = "标题必填" });
        if (req.Count < 1 || req.Count > 500)
            return BadRequest(new { code = "InvalidCount", message = "数量需在 1 到 500 之间" });

        var (errCode, errMsg) = ValidateDiscount(req.FaceValue, req.DiscountPercent);
        if (errCode is not null) return BadRequest(new { code = errCode, message = errMsg });

        // 先生成 Count 个去重码（内存层面），再一次性查 DB 排除已存在；
        // 字符空间 30^8≈6.5e11，N=500 内碰撞概率极低，单轮 1.05× 容量内基本能凑齐。
        var pool = new HashSet<string>(req.Count * 2);
        var safety = 0;
        while (pool.Count < req.Count)
        {
            pool.Add(GenerateCode(kind));
            if (++safety > req.Count * 20)
                return Problem("内存层券码生成连续碰撞，重试或减少数量", statusCode: 500);
        }

        var codes = pool.ToList();
        var clashed = await _db.Vouchers
            .Where(v => codes.Contains(v.Code))
            .Select(v => v.Code)
            .ToListAsync(ct);
        if (clashed.Count > 0)
        {
            // 撞库现有券码：临时补一轮新码替换冲突项；仍冲突则放弃，让前端重试
            var remain = codes.Except(clashed).ToList();
            var extras = new HashSet<string>(remain);
            safety = 0;
            while (extras.Count < req.Count)
            {
                extras.Add(GenerateCode(kind));
                if (++safety > req.Count * 20) break;
            }
            codes = extras.Take(req.Count).ToList();
            if (codes.Count < req.Count)
                return Conflict(new { code = "CodeCollision", message = "券码连续撞库，请重试" });
        }

        var now = DateTime.UtcNow;
        var entities = codes.Select(c => new Voucher
        {
            Kind = kind,
            Code = c,
            Title = req.Title.Trim(),
            FaceValue = req.FaceValue,
            MinOrderAmount = req.MinOrderAmount,
            DiscountPercent = req.DiscountPercent,
            ValidFrom = req.ValidFrom,
            ExpiresAt = req.ExpiresAt,
            Platform = req.Platform,
            Remark = req.Remark,
            Status = VoucherStatus.Active,
            CreatedAt = now
        }).ToList();

        _db.Vouchers.AddRange(entities);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Voucher batch created: kind={Kind} count={Count}", req.Kind, entities.Count);
        return Ok(new BatchCreateVoucherResponse(entities.Count, codes));
    }

    /// <summary>
    /// 按券码取券详情，给收银台在 redeem 之前做预览/校验用。
    /// 不改任何状态；过期券也照样返回，前端按 Status / ExpiresAt 自行展示。
    /// </summary>
    [HttpGet("by-code/{code}")]
    public async Task<ActionResult<VoucherDto>> GetByCode(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return BadRequest(new { code = "InvalidCode", message = "请填写券码" });
        var v = await _db.Vouchers.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code.Trim(), ct);
        if (v is null) return NotFound(new { code = "VoucherNotFound", message = "券不存在" });
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

    /// <summary>
    /// 批量作废：仅对 Active 状态生效；已核销/已作废/已过期 / 不存在的 id 计入 Skipped。
    /// 单事务批量更新，便于在大批量发的码全清场时一次性置废。
    /// </summary>
    [HttpPost("bulk-cancel")]
    public async Task<ActionResult<BulkVoucherActionResponse>> BulkCancel(
        [FromBody] BulkVoucherActionRequest req, CancellationToken ct)
    {
        if (req?.Ids is null || req.Ids.Count == 0)
            return BadRequest(new { code = "InvalidInput", message = "请选择要作废的券" });
        if (req.Ids.Count > 500)
            return BadRequest(new { code = "TooMany", message = "一次最多处理 500 条" });

        var ids = req.Ids.Distinct().ToList();
        var rows = await _db.Vouchers.Where(v => ids.Contains(v.Id)).ToListAsync(ct);
        var found = rows.ToDictionary(v => v.Id);
        var skipped = new List<BulkVoucherSkip>();
        var affected = 0;

        foreach (var id in ids)
        {
            if (!found.TryGetValue(id, out var v))
            {
                skipped.Add(new BulkVoucherSkip(id, null, "NotFound", "券不存在"));
                continue;
            }
            if (v.Status != VoucherStatus.Active)
            {
                var reason = v.Status switch
                {
                    VoucherStatus.Redeemed => "已核销券不可作废",
                    VoucherStatus.Cancelled => "已作废，无需重复操作",
                    VoucherStatus.Expired => "已过期券不可作废",
                    _ => $"状态 {v.Status} 不可作废"
                };
                skipped.Add(new BulkVoucherSkip(id, v.Code, v.Status.ToString(), reason));
                continue;
            }
            v.Status = VoucherStatus.Cancelled;
            affected++;
        }
        if (affected > 0) await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Voucher bulk-cancel: affected={Affected} skipped={Skipped}", affected, skipped.Count);
        return Ok(new BulkVoucherActionResponse(affected, skipped));
    }

    /// <summary>
    /// 批量删除（物理删除）：仅对 Cancelled 生效；其它状态 / 不存在的 id 计入 Skipped。
    /// 业务约束：删除只是清台账噪音；要"撤回券"必须走 Cancel 流程。
    /// </summary>
    [HttpPost("bulk-delete")]
    public async Task<ActionResult<BulkVoucherActionResponse>> BulkDelete(
        [FromBody] BulkVoucherActionRequest req, CancellationToken ct)
    {
        if (req?.Ids is null || req.Ids.Count == 0)
            return BadRequest(new { code = "InvalidInput", message = "请选择要删除的券" });
        if (req.Ids.Count > 500)
            return BadRequest(new { code = "TooMany", message = "一次最多处理 500 条" });

        var ids = req.Ids.Distinct().ToList();
        var rows = await _db.Vouchers.Where(v => ids.Contains(v.Id)).ToListAsync(ct);
        var found = rows.ToDictionary(v => v.Id);
        var skipped = new List<BulkVoucherSkip>();
        var toRemove = new List<Voucher>();

        foreach (var id in ids)
        {
            if (!found.TryGetValue(id, out var v))
            {
                skipped.Add(new BulkVoucherSkip(id, null, "NotFound", "券不存在"));
                continue;
            }
            if (v.Status != VoucherStatus.Cancelled)
            {
                var reason = v.Status switch
                {
                    VoucherStatus.Active => "未作废券不能删除，请先作废",
                    VoucherStatus.Redeemed => "已核销券不能删除（保留台账）",
                    VoucherStatus.Expired => "过期券不能删除，请先作废",
                    _ => $"状态 {v.Status} 不可删除"
                };
                skipped.Add(new BulkVoucherSkip(id, v.Code, v.Status.ToString(), reason));
                continue;
            }
            toRemove.Add(v);
        }
        if (toRemove.Count > 0)
        {
            _db.Vouchers.RemoveRange(toRemove);
            await _db.SaveChangesAsync(ct);
        }
        _logger.LogWarning("Voucher bulk-delete: removed={Removed} skipped={Skipped}", toRemove.Count, skipped.Count);
        return Ok(new BulkVoucherActionResponse(toRemove.Count, skipped));
    }

    private static VoucherDto MapDto(Voucher v) => new(
        v.Id, v.Kind.ToString(), v.Code, v.Title,
        v.FaceValue, v.MinOrderAmount, v.DiscountPercent,
        v.ValidFrom, v.ExpiresAt, v.Status.ToString(),
        v.Platform, v.Remark,
        v.RedeemedAt, v.RedeemedOrderId, v.CreatedAt);
}
