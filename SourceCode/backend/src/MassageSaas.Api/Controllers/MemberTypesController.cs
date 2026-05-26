using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

/// <summary>
/// 会员类型（卡模板）。两种 Kind：StoredValue（充值卡）/ CountBased（计次卡）。
/// 租户自治：店主自由定义可售卡种规格。开卡接口在 MembersController.IssueCard。
/// </summary>
[ApiController]
[Route("api/member-types")]
[Authorize]
public class MemberTypesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<MemberTypesController> _logger;

    public MemberTypesController(
        ApplicationDbContext db,
        ITenantContext tenantContext,
        ILogger<MemberTypesController> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = "ShopStaff")] // 列表对收银员也可见：开卡选模板需要
    public async Task<ActionResult<IReadOnlyList<MemberTypeDto>>> List(
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? kind = null,
        CancellationToken ct = default)
    {
        var q = _db.MemberTypes.AsNoTracking()
            .Include(t => t.ServiceItem)
            .AsQueryable();
        if (!includeInactive) q = q.Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(kind) && Enum.TryParse<MemberTypeKind>(kind, true, out var k))
            q = q.Where(c => c.Kind == k);

        var rows = await q.OrderBy(c => c.Sort).ThenBy(c => c.Id).ToListAsync(ct);
        return Ok(rows.Select(Map).ToList());
    }

    [HttpGet("{id:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<MemberTypeDto>> Get(long id, CancellationToken ct)
    {
        var t = await _db.MemberTypes.AsNoTracking()
            .Include(x => x.ServiceItem)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        return Ok(Map(t));
    }

    [HttpPost]
    [Authorize(Policy = "ShopLeadership")]
    public async Task<ActionResult<MemberTypeDto>> Create(
        [FromBody] CreateMemberTypeRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<MemberTypeKind>(req.Kind, true, out var kind))
            return BadRequest(new { code = "InvalidKind", message = "Kind 必须是 StoredValue 或 CountBased" });

        if (await ValidateAsync(req.Code, kind, req.Discount, req.ServiceItemId,
                req.MinRechargeAmount, req.MinPurchaseCount, req.BonusAmount, req.BonusCount,
                req.ValidDays, null, ct) is { } badResp) return badResp;

        var t = new MemberType
        {
            Code = req.Code.Trim().ToUpperInvariant(),
            Name = req.Name.Trim(),
            Sort = req.Sort,
            Kind = kind,
            ServiceItemId = kind == MemberTypeKind.CountBased ? req.ServiceItemId : null,
            MinRechargeAmount = kind == MemberTypeKind.StoredValue ? req.MinRechargeAmount : null,
            MinPurchaseCount = kind == MemberTypeKind.CountBased ? req.MinPurchaseCount : null,
            Discount = req.Discount,
            BonusAmount = kind == MemberTypeKind.StoredValue ? req.BonusAmount : null,
            BonusCount = kind == MemberTypeKind.CountBased ? req.BonusCount : null,
            ValidDays = req.ValidDays,
            IsActive = req.IsActive,
            Remark = string.IsNullOrWhiteSpace(req.Remark) ? null : req.Remark!.Trim()
        };
        _db.MemberTypes.Add(t);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(t).Reference(x => x.ServiceItem).LoadAsync(ct);
        _logger.LogInformation("Created member type {Id} code={Code} kind={Kind} tenant={TenantId}",
            t.Id, t.Code, t.Kind, _tenantContext.TenantId);
        return CreatedAtAction(nameof(Get), new { id = t.Id }, Map(t));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = "ShopLeadership")]
    public async Task<ActionResult<MemberTypeDto>> Update(
        long id, [FromBody] UpdateMemberTypeRequest req, CancellationToken ct)
    {
        var t = await _db.MemberTypes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();

        // Kind 不可改：改 Kind 等于换卡种语义，应新建一种
        if (await ValidateAsync(t.Code, t.Kind, req.Discount, req.ServiceItemId,
                req.MinRechargeAmount, req.MinPurchaseCount, req.BonusAmount, req.BonusCount,
                req.ValidDays, id, ct) is { } badResp) return badResp;

        t.Name = req.Name.Trim();
        t.Sort = req.Sort;
        t.ServiceItemId = t.Kind == MemberTypeKind.CountBased ? req.ServiceItemId : null;
        t.MinRechargeAmount = t.Kind == MemberTypeKind.StoredValue ? req.MinRechargeAmount : null;
        t.MinPurchaseCount = t.Kind == MemberTypeKind.CountBased ? req.MinPurchaseCount : null;
        t.Discount = req.Discount;
        t.BonusAmount = t.Kind == MemberTypeKind.StoredValue ? req.BonusAmount : null;
        t.BonusCount = t.Kind == MemberTypeKind.CountBased ? req.BonusCount : null;
        t.ValidDays = req.ValidDays;
        t.IsActive = req.IsActive;
        t.Remark = string.IsNullOrWhiteSpace(req.Remark) ? null : req.Remark!.Trim();

        await _db.SaveChangesAsync(ct);
        await _db.Entry(t).Reference(x => x.ServiceItem).LoadAsync(ct);
        return Ok(Map(t));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = "ShopLeadership")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var t = await _db.MemberTypes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();

        // 软删：仅置 IsDeleted/IsActive。已开出的 MemberPackage 与历史 MemberRechargeRecord 无外键
        // 指向 MemberType，不会受影响。后续如果要恢复，可以让用户重新创建同 Code。
        t.IsDeleted = true;
        t.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---- 私有辅助 ----

    private async Task<ActionResult?> ValidateAsync(
        string code, MemberTypeKind kind, decimal discount,
        long? serviceItemId, decimal? minRechargeAmount, int? minPurchaseCount,
        decimal? bonusAmount, int? bonusCount, int? validDays,
        long? excludeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { code = "InvalidCode", message = "Code 不能为空" });
        if (discount <= 0m || discount > 1m)
            return BadRequest(new { code = "InvalidDiscount", message = "Discount 必须在 (0, 1]" });

        if (kind == MemberTypeKind.StoredValue)
        {
            if (minRechargeAmount is null || minRechargeAmount.Value <= 0)
                return BadRequest(new { code = "MissingMinAmount", message = "充值卡必须设置最低充值金额" });
            if (bonusAmount.HasValue && bonusAmount.Value < 0)
                return BadRequest(new { code = "InvalidBonusAmount", message = "赠送金额不能为负" });
        }
        else // CountBased
        {
            if (serviceItemId is null)
                return BadRequest(new { code = "MissingService", message = "计次卡必须选择服务项目" });
            var svcOk = await _db.ServiceItems.AnyAsync(s => s.Id == serviceItemId.Value, ct);
            if (!svcOk) return BadRequest(new { code = "ServiceNotFound", message = "服务项目不存在" });
            if (minPurchaseCount is null || minPurchaseCount.Value <= 0)
                return BadRequest(new { code = "MissingMinCount", message = "计次卡必须设置最低购买次数" });
            if (bonusCount.HasValue && bonusCount.Value < 0)
                return BadRequest(new { code = "InvalidBonusCount", message = "赠送次数不能为负" });
        }

        if (validDays.HasValue && validDays.Value <= 0)
            return BadRequest(new { code = "InvalidValidDays", message = "有效天数必须 > 0；不限请留空" });

        var upper = code.Trim().ToUpperInvariant();
        var dup = await _db.MemberTypes
            .AnyAsync(c => c.Code == upper && (excludeId == null || c.Id != excludeId), ct);
        if (dup) return Conflict(new { code = "DuplicateCode", message = $"Code 已存在: {upper}" });

        return null;
    }

    private static MemberTypeDto Map(MemberType t) => new(
        t.Id, t.Code, t.Name, t.Sort, t.Kind.ToString(),
        t.ServiceItemId, t.ServiceItem?.Name,
        t.MinRechargeAmount, t.MinPurchaseCount,
        t.Discount, t.BonusAmount, t.BonusCount, t.ValidDays,
        t.IsActive, t.Remark, t.CreatedAt);
}
