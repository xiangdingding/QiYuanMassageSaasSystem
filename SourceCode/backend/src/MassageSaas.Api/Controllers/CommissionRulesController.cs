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

    /// <summary>解析规则维度的 source：留空/Unknown/无效 → null（通配两种来源）。
    /// 注意与 Orders 维度不同：Orders 的 OrderItem.AssignmentSource 必须明确为 Rotation/Designation，
    /// 这里规则维度允许 null 让一条规则同时覆盖轮钟和点钟。</summary>
    private static AssignmentSource? ParseRuleSource(string? s) =>
        Enum.TryParse<AssignmentSource>(s, true, out var v) && v != AssignmentSource.Unknown
            ? v
            : (AssignmentSource?)null;

    private static bool SupportsDualAmount(CommissionRuleType rt) =>
        rt is CommissionRuleType.FixedAmount or CommissionRuleType.Percentage;

    /// <summary>校验双金额字段：仅 FixedAmount/Percentage 允许填，并不能为负。</summary>
    private static string? ValidateDualAmount(CommissionRuleType rt, decimal? rotation, decimal? designation)
    {
        if ((rotation.HasValue || designation.HasValue) && !SupportsDualAmount(rt))
            return "轮钟/点钟独立金额仅 固定金额、百分比 类型支持。阶梯/按时计费请用单一金额。";
        if (rotation is < 0 || designation is < 0)
            return "金额不能为负。";
        return null;
    }

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
                r.IsActive,
                r.AssignmentSource.HasValue ? r.AssignmentSource.Value.ToString() : null,
                r.RotationAmount,
                r.DesignationAmount))
            .ToListAsync(ct);

        return Ok(data);
    }

    [HttpPost]
    public async Task<ActionResult<CommissionRuleDto>> Create([FromBody] CreateCommissionRuleRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<CommissionRuleType>(req.RuleType, true, out var rt))
            return BadRequest(new { code = "InvalidRuleType", message = "规则类型不合法" });
        var err = ValidateDualAmount(rt, req.RotationAmount, req.DesignationAmount);
        if (err != null) return BadRequest(new { code = "InvalidDualAmount", message = err });

        var entity = new CommissionRule
        {
            ServiceId = req.ServiceId,
            TechnicianId = req.TechnicianId,
            RuleType = rt,
            Amount = req.Amount,
            TieredRulesJson = req.TieredRulesJson,
            Priority = req.Priority,
            IsActive = req.IsActive,
            AssignmentSource = ParseRuleSource(req.AssignmentSource),
            RotationAmount = SupportsDualAmount(rt) ? req.RotationAmount : null,
            DesignationAmount = SupportsDualAmount(rt) ? req.DesignationAmount : null
        };
        _db.CommissionRules.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new CommissionRuleDto(
            entity.Id, entity.ServiceId, null, entity.TechnicianId, null,
            entity.RuleType.ToString(), entity.Amount, entity.TieredRulesJson,
            entity.Priority, entity.IsActive,
            entity.AssignmentSource.HasValue ? entity.AssignmentSource.Value.ToString() : null,
            entity.RotationAmount, entity.DesignationAmount));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CommissionRuleDto>> Update(long id, [FromBody] UpdateCommissionRuleRequest req, CancellationToken ct)
    {
        var rule = await _db.CommissionRules.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (rule is null) return NotFound();
        if (!Enum.TryParse<CommissionRuleType>(req.RuleType, true, out var rt))
            return BadRequest(new { code = "InvalidRuleType", message = "规则类型不合法" });
        var err = ValidateDualAmount(rt, req.RotationAmount, req.DesignationAmount);
        if (err != null) return BadRequest(new { code = "InvalidDualAmount", message = err });

        rule.RuleType = rt;
        rule.Amount = req.Amount;
        rule.TieredRulesJson = req.TieredRulesJson;
        rule.Priority = req.Priority;
        rule.IsActive = req.IsActive;
        rule.AssignmentSource = ParseRuleSource(req.AssignmentSource);
        rule.RotationAmount = SupportsDualAmount(rt) ? req.RotationAmount : null;
        rule.DesignationAmount = SupportsDualAmount(rt) ? req.DesignationAmount : null;
        await _db.SaveChangesAsync(ct);
        return Ok(new CommissionRuleDto(
            rule.Id, rule.ServiceId, null, rule.TechnicianId, null,
            rule.RuleType.ToString(), rule.Amount, rule.TieredRulesJson,
            rule.Priority, rule.IsActive,
            rule.AssignmentSource.HasValue ? rule.AssignmentSource.Value.ToString() : null,
            rule.RotationAmount, rule.DesignationAmount));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var rule = await _db.CommissionRules.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (rule is null) return NotFound();
        if (rule.IsActive)
            return BadRequest(new { code = "ActiveCannotDelete", message = "请先禁用该提成规则，再删除。" });
        rule.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>批量启用/禁用：把选中的规则统一置为启用或停用。</summary>
    [HttpPut("bulk-status")]
    public async Task<ActionResult<BulkCommissionStatusResult>> BulkStatus(
        [FromBody] BulkCommissionStatusRequest req, CancellationToken ct)
    {
        if (req.Ids is null || req.Ids.Length == 0)
            return BadRequest(new { code = "NoRules", message = "请至少选择一条提成规则" });

        var rules = await _db.CommissionRules
            .Where(r => req.Ids.Contains(r.Id))
            .ToListAsync(ct);
        foreach (var r in rules) r.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(new BulkCommissionStatusResult(rules.Count));
    }

    /// <summary>批量删除：软删除选中的规则。与单条删除一致，启用中的规则跳过（需先禁用）。</summary>
    [HttpPost("bulk-delete")]
    public async Task<ActionResult<BulkCommissionDeleteResult>> BulkDelete(
        [FromBody] BulkCommissionDeleteRequest req, CancellationToken ct)
    {
        if (req.Ids is null || req.Ids.Length == 0)
            return BadRequest(new { code = "NoRules", message = "请至少选择一条提成规则" });

        var rules = await _db.CommissionRules
            .Where(r => req.Ids.Contains(r.Id))
            .ToListAsync(ct);

        int deleted = 0, skippedActive = 0;
        foreach (var r in rules)
        {
            if (r.IsActive) { skippedActive++; continue; }
            r.IsDeleted = true;
            deleted++;
        }
        await _db.SaveChangesAsync(ct);
        return Ok(new BulkCommissionDeleteResult(deleted, skippedActive));
    }

    /// <summary>
    /// 批量配置：对 ServiceIds × TechnicianIds 的笛卡尔积，按同一份模板生成/更新通用规则
    /// （仅匹配 AssignmentSource == null 的旧规则，避免误覆盖 source-specific 规则）。
    /// </summary>
    [HttpPost("bulk")]
    public async Task<ActionResult<BulkCommissionRuleResult>> Bulk(
        [FromBody] BulkCommissionRuleRequest req, CancellationToken ct)
    {
        if (req.ServiceIds is null || req.ServiceIds.Length == 0)
            return BadRequest(new { code = "NoServices", message = "请至少选择一个服务项目" });
        if (req.TechnicianIds is null || req.TechnicianIds.Length == 0)
            return BadRequest(new { code = "NoTechnicians", message = "请至少选择一个技师" });
        if (!Enum.TryParse<CommissionRuleType>(req.RuleType, true, out var rt))
            return BadRequest(new { code = "InvalidRuleType", message = "规则类型不合法" });
        var err = ValidateDualAmount(rt, req.RotationAmount, req.DesignationAmount);
        if (err != null) return BadRequest(new { code = "InvalidDualAmount", message = err });

        var validServiceIds = await _db.ServiceItems.AsNoTracking()
            .Where(s => req.ServiceIds.Contains(s.Id))
            .Select(s => s.Id).ToListAsync(ct);
        var validTechnicianIds = await _db.Users.AsNoTracking()
            .Where(u => req.TechnicianIds.Contains(u.Id) && u.Role == UserRole.Technician)
            .Select(u => u.Id).ToListAsync(ct);

        var pairs = validServiceIds
            .SelectMany(sid => validTechnicianIds.Select(tid => new { Sid = sid, Tid = tid }))
            .ToList();
        if (pairs.Count == 0)
            return BadRequest(new { code = "NoValidPair", message = "选中的服务/技师在当前租户下不存在" });

        var sidSet = validServiceIds.ToHashSet();
        var tidSet = validTechnicianIds.ToHashSet();
        var existing = await _db.CommissionRules
            .Where(r => r.ServiceId != null && r.TechnicianId != null
                        && r.AssignmentSource == null
                        && sidSet.Contains(r.ServiceId!.Value)
                        && tidSet.Contains(r.TechnicianId!.Value))
            .ToListAsync(ct);
        // 同一(服务,技师,source=null)历史上可能存在多条规则；按分组取一条作为规范规则，
        // 多余的软删除清理掉，既避免 ToDictionary 重复键 500，又消除规则歧义。
        var existingMap = new Dictionary<(long, long), CommissionRule>();
        foreach (var g in existing.GroupBy(r => (r.ServiceId!.Value, r.TechnicianId!.Value)))
        {
            var ordered = g.OrderByDescending(r => r.Priority).ThenByDescending(r => r.Id).ToList();
            existingMap[g.Key] = ordered[0];
            for (var i = 1; i < ordered.Count; i++) ordered[i].IsDeleted = true;
        }

        int created = 0, updated = 0, skipped = 0;
        var supportsDual = SupportsDualAmount(rt);

        foreach (var p in pairs)
        {
            var sid = p.Sid; var tid = p.Tid;
            if (existingMap.TryGetValue((sid, tid), out var existRule))
            {
                if (!req.OverwriteExisting) { skipped++; continue; }
                existRule.RuleType = rt;
                existRule.Amount = req.Amount;
                existRule.TieredRulesJson = req.TieredRulesJson;
                existRule.Priority = req.Priority;
                existRule.IsActive = req.IsActive;
                existRule.RotationAmount = supportsDual ? req.RotationAmount : null;
                existRule.DesignationAmount = supportsDual ? req.DesignationAmount : null;
                existRule.IsDeleted = false;
                updated++;
            }
            else
            {
                _db.CommissionRules.Add(new CommissionRule
                {
                    ServiceId = sid,
                    TechnicianId = tid,
                    RuleType = rt,
                    Amount = req.Amount,
                    TieredRulesJson = req.TieredRulesJson,
                    Priority = req.Priority,
                    IsActive = req.IsActive,
                    AssignmentSource = null,
                    RotationAmount = supportsDual ? req.RotationAmount : null,
                    DesignationAmount = supportsDual ? req.DesignationAmount : null
                });
                created++;
            }
        }
        await _db.SaveChangesAsync(ct);
        return Ok(new BulkCommissionRuleResult(created, updated, skipped));
    }
}
