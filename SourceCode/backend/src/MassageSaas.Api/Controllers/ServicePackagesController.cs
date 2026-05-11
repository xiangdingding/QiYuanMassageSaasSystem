using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.ServicePackages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/service-packages")]
[Authorize]
public class ServicePackagesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ServicePackagesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServicePackageDto>>> List(
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var q = _db.ServicePackages.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Service)
            .AsQueryable();
        if (!includeInactive) q = q.Where(p => p.IsActive);
        var rows = await q.OrderBy(p => p.Code).ToListAsync(ct);
        return Ok(rows.Select(MapDto).ToList());
    }

    [HttpPost]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<ServicePackageDto>> Create(
        [FromBody] CreateServicePackageRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { code = "InvalidInput", message = "编码与名称必填" });
        if (req.Items is null || req.Items.Count == 0)
            return BadRequest(new { code = "EmptyItems", message = "套餐至少包含一项服务" });
        var dup = await _db.ServicePackages.AnyAsync(p => p.Code == req.Code, ct);
        if (dup) return Conflict(new { code = "DuplicateCode", message = "套餐编码已存在" });

        var svcIds = req.Items.Select(i => i.ServiceId).ToList();
        var exist = await _db.ServiceItems.CountAsync(s => svcIds.Contains(s.Id) && s.IsActive, ct);
        if (exist != svcIds.Distinct().Count())
            return BadRequest(new { code = "ServiceInvalid", message = "存在不存在或已停用的服务项" });

        var pkg = new ServicePackage
        {
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            Price = req.Price,
            MemberPrice = req.MemberPrice,
            Description = req.Description,
            IsActive = true
        };
        foreach (var it in req.Items)
        {
            pkg.Items.Add(new ServicePackageItem
            {
                ServiceId = it.ServiceId,
                Quantity = it.Quantity < 1 ? 1 : it.Quantity
            });
        }
        _db.ServicePackages.Add(pkg);
        await _db.SaveChangesAsync(ct);

        var saved = await _db.ServicePackages.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Service)
            .FirstAsync(p => p.Id == pkg.Id, ct);
        return Ok(MapDto(saved));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<ServicePackageDto>> Update(
        long id, [FromBody] UpdateServicePackageRequest req, CancellationToken ct)
    {
        var pkg = await _db.ServicePackages.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pkg is null) return NotFound();

        pkg.Name = req.Name.Trim();
        pkg.Price = req.Price;
        pkg.MemberPrice = req.MemberPrice;
        pkg.Description = req.Description;
        pkg.IsActive = req.IsActive;

        _db.ServicePackageItems.RemoveRange(pkg.Items);
        pkg.Items.Clear();
        foreach (var it in req.Items)
        {
            pkg.Items.Add(new ServicePackageItem
            {
                ServiceId = it.ServiceId,
                Quantity = it.Quantity < 1 ? 1 : it.Quantity
            });
        }
        await _db.SaveChangesAsync(ct);

        var saved = await _db.ServicePackages.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Service)
            .FirstAsync(p => p.Id == id, ct);
        return Ok(MapDto(saved));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var pkg = await _db.ServicePackages.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pkg is null) return NotFound();
        pkg.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static ServicePackageDto MapDto(ServicePackage p) => new(
        p.Id, p.Code, p.Name, p.Price, p.MemberPrice, p.Description, p.IsActive,
        p.Items.Select(i => new ServicePackageItemDto(
            i.ServiceId,
            i.Service?.Name ?? string.Empty,
            i.Quantity)).ToList());
}
