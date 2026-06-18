using MassageSaas.Api.Content;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

/// <summary>
/// 平台级全局配置。目前提供"服务订阅"页的到期说明与客服联系方式：
/// 平台端（PlatformAdmin）维护，各租户端（BS/CS）只读获取后展示。
/// </summary>
[ApiController]
[Route("api/platform-settings")]
public class PlatformSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PlatformSettingsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // 平台端未配置时的兜底文案，保证各端始终有内容可展示。
    private const string DefaultNotice =
        "订阅到期后，系统进入只读模式：只能查询数据，无法新建订单、充值或编辑配置。\n" +
        "新购年限从当前到期日继续累加；如已过期，则从今天起算。";

    /// <summary>
    /// 获取"服务订阅"展示配置。任意已登录用户可读（含已到期租户——GET 读请求放行），
    /// 供 BS/CS 自动拉取展示。
    /// </summary>
    [HttpGet("subscription")]
    [Authorize]
    public async Task<ActionResult<PlatformSubscriptionSettingDto>> GetSubscription(CancellationToken ct)
    {
        var s = await _db.PlatformSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        return Ok(new PlatformSubscriptionSettingDto(
            string.IsNullOrWhiteSpace(s?.ExpiryNotice) ? DefaultNotice : s!.ExpiryNotice,
            s?.ContactPhone,
            s?.ContactWechat));
    }

    /// <summary>平台端维护"服务订阅"展示配置（说明 + 客服电话 + 客服微信）。</summary>
    [HttpPut("subscription")]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<ActionResult<PlatformSubscriptionSettingDto>> UpdateSubscription(
        [FromBody] UpdatePlatformSubscriptionSettingRequest req, CancellationToken ct)
    {
        var s = await _db.PlatformSettings.FirstOrDefaultAsync(ct);
        if (s is null)
        {
            s = new PlatformSetting();
            _db.PlatformSettings.Add(s);
        }

        s.ExpiryNotice = req.ExpiryNotice?.Trim();
        s.ContactPhone = req.ContactPhone?.Trim();
        s.ContactWechat = req.ContactWechat?.Trim();
        await _db.SaveChangesAsync(ct);

        return Ok(new PlatformSubscriptionSettingDto(
            string.IsNullOrWhiteSpace(s.ExpiryNotice) ? DefaultNotice : s.ExpiryNotice,
            s.ContactPhone,
            s.ContactWechat));
    }

    /// <summary>
    /// 获取用户使用说明书（CS + BS 两份）。任意已登录用户可读（含已到期租户），
    /// 供各端 F1 帮助展示。平台端未维护时回退内置默认手册。
    /// </summary>
    [HttpGet("manual")]
    [Authorize]
    public async Task<ActionResult<PlatformManualDto>> GetManual(
        [FromQuery] bool raw = false, CancellationToken ct = default)
    {
        var s = await _db.PlatformSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        // raw=true 给平台端编辑器：返回库里真实存的值（未维护则空串），便于"留空即用默认"。
        // raw=false 给各端 F1 帮助：未维护处回退内置默认手册。
        if (raw)
            return Ok(new PlatformManualDto(
                s?.CsManualNormal ?? "", s?.CsManualA11y ?? "",
                s?.BsManualNormal ?? "", s?.BsManualA11y ?? ""));
        return Ok(Compose(s));
    }

    /// <summary>
    /// 平台端维护用户使用说明书：CS / BS × 正常 / 无障碍 共四份。某份留空则该份回退内置默认手册。
    /// </summary>
    [HttpPut("manual")]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<ActionResult<PlatformManualDto>> UpdateManual(
        [FromBody] UpdatePlatformManualRequest req, CancellationToken ct)
    {
        var s = await _db.PlatformSettings.FirstOrDefaultAsync(ct);
        if (s is null)
        {
            s = new PlatformSetting();
            _db.PlatformSettings.Add(s);
        }

        s.CsManualNormal = Blank(req.CsManualNormal);
        s.CsManualA11y = Blank(req.CsManualA11y);
        s.BsManualNormal = Blank(req.BsManualNormal);
        s.BsManualA11y = Blank(req.BsManualA11y);
        await _db.SaveChangesAsync(ct);

        return Ok(Compose(s));

        static string? Blank(string? v) => string.IsNullOrWhiteSpace(v) ? null : v;
    }

    // 四份手册：平台端有维护用维护值，否则回退内置默认手册，保证各端始终有完整内容可读。
    private static PlatformManualDto Compose(PlatformSetting? s) => new(
        string.IsNullOrWhiteSpace(s?.CsManualNormal) ? DefaultManuals.CsNormal : s!.CsManualNormal,
        string.IsNullOrWhiteSpace(s?.CsManualA11y) ? DefaultManuals.CsA11y : s!.CsManualA11y,
        string.IsNullOrWhiteSpace(s?.BsManualNormal) ? DefaultManuals.BsNormal : s!.BsManualNormal,
        string.IsNullOrWhiteSpace(s?.BsManualA11y) ? DefaultManuals.BsA11y : s!.BsManualA11y);

    /// <summary>
    /// 获取《用户服务协议》《隐私协议》。注册页在登录前展示，故匿名可读；
    /// raw=true 给平台端编辑器取原始值（未维护则空串），否则回退内置默认协议。
    /// </summary>
    [HttpGet("agreements")]
    [AllowAnonymous]
    public async Task<ActionResult<PlatformAgreementDto>> GetAgreements(
        [FromQuery] bool raw = false, CancellationToken ct = default)
    {
        var s = await _db.PlatformSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        if (raw)
            return Ok(new PlatformAgreementDto(s?.ServiceAgreement ?? "", s?.PrivacyPolicy ?? ""));
        return Ok(new PlatformAgreementDto(
            string.IsNullOrWhiteSpace(s?.ServiceAgreement) ? DefaultAgreements.Service : s!.ServiceAgreement,
            string.IsNullOrWhiteSpace(s?.PrivacyPolicy) ? DefaultAgreements.Privacy : s!.PrivacyPolicy));
    }

    /// <summary>平台端维护《用户服务协议》《隐私协议》。留空则注册页回退内置默认协议。</summary>
    [HttpPut("agreements")]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<ActionResult<PlatformAgreementDto>> UpdateAgreements(
        [FromBody] UpdatePlatformAgreementRequest req, CancellationToken ct)
    {
        var s = await _db.PlatformSettings.FirstOrDefaultAsync(ct);
        if (s is null)
        {
            s = new PlatformSetting();
            _db.PlatformSettings.Add(s);
        }

        s.ServiceAgreement = string.IsNullOrWhiteSpace(req.ServiceAgreement) ? null : req.ServiceAgreement;
        s.PrivacyPolicy = string.IsNullOrWhiteSpace(req.PrivacyPolicy) ? null : req.PrivacyPolicy;
        await _db.SaveChangesAsync(ct);

        return Ok(new PlatformAgreementDto(
            string.IsNullOrWhiteSpace(s.ServiceAgreement) ? DefaultAgreements.Service : s.ServiceAgreement,
            string.IsNullOrWhiteSpace(s.PrivacyPolicy) ? DefaultAgreements.Privacy : s.PrivacyPolicy));
    }
}
