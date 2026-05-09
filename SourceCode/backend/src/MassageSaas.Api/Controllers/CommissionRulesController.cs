using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Commissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/commission-rules")]
[Authorize(Policy = "ShopStaff")]
public class CommissionRulesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CommissionRulesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CommissionRuleDto>>> List(
        [FromQuery] long? serviceId,
        [FromQuery] long? technicianId,
        CancellationToken ct = default)
    {
        var q = _db.CommissionRules.AsNoTracking()
            .Include(r => r.Service)
            .Include(r => r.Technician)
            .AsQueryable();
        if (serviceId.HasValue) q = q.Where(r => r.ServiceId == serviceId.Value);
        if (technicianId.HasValue) q = q.Where(r => r.TechnicianId == technicianId.Value);

        var data = await q
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.Id)
            .Select(r => new CommissionRuleDto(
                r.Id,
                r.ServiceId,
                r.Service != null ? r.Service.Name : null,
                r.TechnicianId,
                r.Technician != null ? (r.Technician.RealName ?? r.Technician.Username) : null,
                r.RuleType.ToString(),
                r.Amount,
                r.TieredRulesJson,
                r.Priority,
                r.IsActive))
            .ToListAsync(ct);

        return Ok(data);
    }

    [HttpPost]
    public async Task<ActionResult<CommissionRuleDto>> Create([FromBody] CreateCommissionRuleRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<CommissionRuleType>(req.RuleType, true, out var rt))
            return BadRequest(new { code = "InvalidRuleType", message = "规则类型不合法" });

        var entity = new CommissionRule
        {
            ServiceId = req.ServiceId,
            TechnicianId = req.TechnicianId,
            RuleType = rt,
            Amount = req.Amount,
            TieredRulesJson = req.TieredRulesJson,
            Priority = req.Priority,
            IsActive = req.IsActive
        };
        _db.CommissionRules.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new CommissionRuleDto(
            entity.Id, entity.ServiceId, null, entity.TechnicianId, null,
            entity.RuleType.ToString(), entity.Amount, entity.TieredRulesJson,
            entity.Priority, entity.IsActive));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CommissionRuleDto>> Update(long id, [FromBody] UpdateCommissionRuleRequest req, CancellationToken ct)
    {
        var rule = await _db.CommissionRules.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (rule is null) return NotFound();
        if (!Enum.TryParse<CommissionRuleType>(req.RuleType, true, out var rt))
            return BadRequest(new { code = "InvalidRuleType", message = "规则类型不合法" });

        rule.RuleType = rt;
        rule.Amount = req.Amount;
        rule.TieredRulesJson = req.TieredRulesJson;
        rule.Priority = req.Priority;
        rule.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(new CommissionRuleDto(
            rule.Id, rule.ServiceId, null, rule.TechnicianId, null,
            rule.RuleType.ToString(), rule.Amount, rule.TieredRulesJson,
            rule.Priority, rule.IsActive));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var rule = await _db.CommissionRules.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (rule is null) return NotFound();
        rule.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
