using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Plans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/plans")]
public class PlansController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public PlansController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<PlanDto>>> List([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.Plans.AsNoTracking();
        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        var data = await query.OrderBy(p => p.AnnualPrice)
            .Select(p => new PlanDto(p.Id, p.Code, p.Name, p.MaxStores, p.MaxStaff, p.AnnualPrice, p.FeatureJson, p.IsActive))
            .ToListAsync(ct);

        return Ok(data);
    }

    [HttpGet("{id:long}")]
    [Authorize]
    public async Task<ActionResult<PlanDto>> Get(long id, CancellationToken ct)
    {
        var plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null) return NotFound();
        return Ok(new PlanDto(plan.Id, plan.Code, plan.Name, plan.MaxStores, plan.MaxStaff, plan.AnnualPrice, plan.FeatureJson, plan.IsActive));
    }

    [HttpPost]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<ActionResult<PlanDto>> Create([FromBody] CreatePlanRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { code = "InvalidInput", message = "套餐编码与名称不能为空" });

        var exists = await _db.Plans.AnyAsync(p => p.Code == req.Code, ct);
        if (exists)
            return Conflict(new { code = "PlanCodeExists", message = $"套餐编码已存在: {req.Code}" });

        var plan = new Plan
        {
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            MaxStores = req.MaxStores,
            MaxStaff = req.MaxStaff,
            AnnualPrice = req.AnnualPrice,
            FeatureJson = req.FeatureJson,
            IsActive = req.IsActive
        };
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = plan.Id },
            new PlanDto(plan.Id, plan.Code, plan.Name, plan.MaxStores, plan.MaxStaff, plan.AnnualPrice, plan.FeatureJson, plan.IsActive));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<ActionResult<PlanDto>> Update(long id, [FromBody] UpdatePlanRequest req, CancellationToken ct)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null) return NotFound();

        plan.Name = req.Name.Trim();
        plan.MaxStores = req.MaxStores;
        plan.MaxStaff = req.MaxStaff;
        plan.AnnualPrice = req.AnnualPrice;
        plan.FeatureJson = req.FeatureJson;
        plan.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);

        return Ok(new PlanDto(plan.Id, plan.Code, plan.Name, plan.MaxStores, plan.MaxStaff, plan.AnnualPrice, plan.FeatureJson, plan.IsActive));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null) return NotFound();

        var inUse = await _db.Tenants.AnyAsync(t => t.CurrentPlanId == id, ct)
                    || await _db.Subscriptions.AnyAsync(s => s.PlanId == id, ct);
        if (inUse)
        {
            plan.IsActive = false;
            await _db.SaveChangesAsync(ct);
            return Ok(new { code = "Deactivated", message = "套餐已被使用，已改为停用而非删除" });
        }

        plan.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
