using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.MemberPackages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/member-packages")]
[Authorize(Policy = "ShopStaff")]
public class MemberPackagesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public MemberPackagesController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MemberPackageDto>>> List(
        [FromQuery] long? memberId = null,
        [FromQuery] long? storeId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var q = _db.MemberPackages.AsNoTracking()
            .Include(p => p.Member)
            .Include(p => p.Service)
            .AsQueryable();
        if (memberId.HasValue) q = q.Where(p => p.MemberId == memberId.Value);
        if (storeId.HasValue) q = q.Where(p => p.StoreId == storeId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MemberPackageStatus>(status, true, out var s))
            q = q.Where(p => p.Status == s);

        var rows = await q.OrderByDescending(p => p.CreatedAt).Take(500).ToListAsync(ct);
        return Ok(rows.Select(MapDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<MemberPackageDto>> Create([FromBody] CreateMemberPackageRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<MemberPackageKind>(req.Kind, true, out var kind))
            return BadRequest(new { code = "InvalidKind", message = "套餐类型不合法" });

        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == req.MemberId, ct);
        if (member is null) return BadRequest(new { code = "MemberNotFound", message = "会员不存在" });

        if (kind == MemberPackageKind.Counter)
        {
            if (req.TotalCount < 1)
                return BadRequest(new { code = "InvalidCount", message = "计次卡总次数必须 >= 1" });
            if (!req.ServiceId.HasValue)
                return BadRequest(new { code = "ServiceRequired", message = "计次卡必须指定服务项" });
        }

        if (req.ServiceId.HasValue)
        {
            var svcExists = await _db.ServiceItems.AnyAsync(s => s.Id == req.ServiceId.Value && s.IsActive, ct);
            if (!svcExists) return BadRequest(new { code = "ServiceNotFound", message = "服务不存在或已停用" });
        }

        var pkg = new MemberPackage
        {
            MemberId = req.MemberId,
            StoreId = req.StoreId,
            Kind = kind,
            ServiceId = req.ServiceId,
            Title = req.Title.Trim(),
            PaidAmount = req.PaidAmount,
            TotalCount = req.TotalCount,
            RemainCount = req.TotalCount,
            ValidFrom = req.ValidFrom,
            ExpiresAt = req.ExpiresAt,
            Remark = req.Remark,
            Status = MemberPackageStatus.Active
        };
        _db.MemberPackages.Add(pkg);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(pkg).Reference(p => p.Member).LoadAsync(ct);
        if (pkg.ServiceId.HasValue)
            await _db.Entry(pkg).Reference(p => p.Service!).LoadAsync(ct);
        return Ok(MapDto(pkg));
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var pkg = await _db.MemberPackages.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pkg is null) return NotFound();
        if (pkg.Status != MemberPackageStatus.Active)
            return BadRequest(new { code = "InvalidState", message = "仅生效中的套餐可作废" });
        pkg.Status = MemberPackageStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static MemberPackageDto MapDto(MemberPackage p) => new(
        p.Id, p.MemberId,
        p.Member?.Name ?? p.Member?.CardNo ?? string.Empty,
        p.StoreId, p.Kind.ToString(),
        p.ServiceId, p.Service?.Name,
        p.Title, p.PaidAmount, p.TotalCount, p.RemainCount,
        p.ValidFrom, p.ExpiresAt, p.Status.ToString(), p.Remark, p.CreatedAt);
}
