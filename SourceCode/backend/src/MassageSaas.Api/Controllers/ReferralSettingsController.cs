using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

/// <summary>
/// 推荐规则配置（全店统一，存于 Tenant）。规则模块"推荐规则" Tab 调用。
/// </summary>
[ApiController]
[Route("api/referral-settings")]
[Authorize(Policy = "ShopStaff")]
public class ReferralSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ReferralSettingsController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<ReferralSettingDto>> Get(CancellationToken ct)
    {
        if (_tenantContext.TenantId is not long tid)
            return Ok(new ReferralSettingDto(CustomerReferralMode.None.ToString(), 0m, 0m, StaffReferralMode.None.ToString(), 0m, 0m));
        var t = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tid, ct);
        if (t is null) return Ok(new ReferralSettingDto(CustomerReferralMode.None.ToString(), 0m, 0m, StaffReferralMode.None.ToString(), 0m, 0m));
        return Ok(new ReferralSettingDto(
            t.CustomerReferralMode.ToString(),
            t.ReferralRewardPercent, t.CustomerReferralFixedReward,
            t.StaffReferralMode.ToString(), t.StaffReferralFixedAmount, t.StaffReferralPercent));
    }

    [HttpPut]
    public async Task<ActionResult<ReferralSettingDto>> Update(
        [FromBody] UpdateReferralSettingRequest req, CancellationToken ct)
    {
        if (_tenantContext.TenantId is not long tid)
            return BadRequest(new { code = "NoTenant", message = "当前账号无租户上下文" });

        if (!Enum.TryParse<CustomerReferralMode>(req.CustomerReferralMode, true, out var customerMode))
            return BadRequest(new { code = "InvalidCustomerMode", message = "顾客推荐返佣模式不合法" });
        if (req.CustomerRewardPercent < 0 || req.CustomerRewardPercent > 100)
            return BadRequest(new { code = "InvalidPercent", message = "顾客返佣百分比需在 0-100 之间" });
        if (req.CustomerFixedReward < 0)
            return BadRequest(new { code = "InvalidFixed", message = "顾客固定推荐费不能为负" });
        if (!Enum.TryParse<StaffReferralMode>(req.StaffReferralMode, true, out var mode))
            return BadRequest(new { code = "InvalidMode", message = "员工推荐提成模式不合法" });
        if (req.StaffReferralFixedAmount < 0)
            return BadRequest(new { code = "InvalidStaffFixed", message = "员工固定推荐提成金额不能为负" });
        if (req.StaffReferralPercent < 0 || req.StaffReferralPercent > 100)
            return BadRequest(new { code = "InvalidStaffPercent", message = "员工推荐提成百分比需在 0-100 之间" });

        var t = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == tid, ct);
        if (t is null) return NotFound(new { code = "TenantNotFound", message = "租户不存在" });

        t.CustomerReferralMode = customerMode;
        t.ReferralRewardPercent = req.CustomerRewardPercent;
        t.CustomerReferralFixedReward = req.CustomerFixedReward;
        t.StaffReferralMode = mode;
        t.StaffReferralFixedAmount = req.StaffReferralFixedAmount;
        t.StaffReferralPercent = req.StaffReferralPercent;
        await _db.SaveChangesAsync(ct);

        return Ok(new ReferralSettingDto(
            t.CustomerReferralMode.ToString(),
            t.ReferralRewardPercent, t.CustomerReferralFixedReward,
            t.StaffReferralMode.ToString(), t.StaffReferralFixedAmount, t.StaffReferralPercent));
    }
}
