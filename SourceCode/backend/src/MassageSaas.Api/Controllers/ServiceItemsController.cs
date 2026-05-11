using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/services")]
[Authorize]
public class ServiceItemsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ServiceItemsController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceItemDto>>> List(
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var q = _db.ServiceItems.AsNoTracking();
        if (!includeInactive) q = q.Where(s => s.IsActive);

        var data = await q.OrderBy(s => s.Code)
            .Select(s => new ServiceItemDto(s.Id, s.Code, s.Name, s.DurationMinutes, s.Price, s.MemberPrice, s.PriceJunior, s.PriceMaster, s.Description, s.IsActive))
            .ToListAsync(ct);
        return Ok(data);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ServiceItemDto>> Get(long id, CancellationToken ct)
    {
        var s = await _db.ServiceItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return NotFound();
        return Ok(new ServiceItemDto(s.Id, s.Code, s.Name, s.DurationMinutes, s.Price, s.MemberPrice, s.PriceJunior, s.PriceMaster, s.Description, s.IsActive));
    }

    [HttpPost]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<ServiceItemDto>> Create([FromBody] CreateServiceItemRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { code = "InvalidInput", message = "编码与名称必填" });
        if (req.DurationMinutes < 1)
            return BadRequest(new { code = "InvalidInput", message = "时长必须大于 0" });

        var exists = await _db.ServiceItems.AnyAsync(x => x.Code == req.Code, ct);
        if (exists) return Conflict(new { code = "DuplicateCode", message = $"服务编码已存在: {req.Code}" });

        var entity = new ServiceItem
        {
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            DurationMinutes = req.DurationMinutes,
            Price = req.Price,
            MemberPrice = req.MemberPrice,
            PriceJunior = req.PriceJunior,
            PriceMaster = req.PriceMaster,
            Description = req.Description,
            IsActive = req.IsActive
        };
        _db.ServiceItems.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = entity.Id },
            new ServiceItemDto(entity.Id, entity.Code, entity.Name, entity.DurationMinutes, entity.Price, entity.MemberPrice, entity.PriceJunior, entity.PriceMaster, entity.Description, entity.IsActive));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<ServiceItemDto>> Update(long id, [FromBody] UpdateServiceItemRequest req, CancellationToken ct)
    {
        var s = await _db.ServiceItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return NotFound();

        s.Name = req.Name.Trim();
        s.DurationMinutes = req.DurationMinutes;
        s.Price = req.Price;
        s.MemberPrice = req.MemberPrice;
        s.PriceJunior = req.PriceJunior;
        s.PriceMaster = req.PriceMaster;
        s.Description = req.Description;
        s.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(new ServiceItemDto(s.Id, s.Code, s.Name, s.DurationMinutes, s.Price, s.MemberPrice, s.PriceJunior, s.PriceMaster, s.Description, s.IsActive));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var s = await _db.ServiceItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return NotFound();

        var inUse = await _db.OrderItems.AnyAsync(x => x.ServiceId == id, ct);
        if (inUse)
        {
            s.IsActive = false;
            await _db.SaveChangesAsync(ct);
            return Ok(new { code = "Deactivated", message = "已被订单引用，已转为停用" });
        }

        s.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
