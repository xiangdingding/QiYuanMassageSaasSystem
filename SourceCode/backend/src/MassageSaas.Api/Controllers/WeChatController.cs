using MassageSaas.Application.Abstractions;
using MassageSaas.Infrastructure.Notifications;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.WeChat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MassageSaas.Api.Controllers;

/// <summary>
/// 微信小程序对接：登录换 OpenId、会员卡绑定。
/// 全部匿名可用——顾客在小程序里没有店员账号。绑定后生日/到期/充值等通知
/// 才会带上 RecipientOpenId，订阅消息通道才有下发对象。
/// </summary>
[ApiController]
[Route("api/wechat")]
[AllowAnonymous]
public class WeChatController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IWeChatMiniProgramClient _wechat;
    private readonly WeChatOptions _options;
    private readonly ILogger<WeChatController> _logger;

    /// <summary>面向顾客的订阅消息类型。投诉提醒是发给店长的，不在此列。</summary>
    private static readonly string[] CustomerNotificationKinds =
        { "AppointmentReminder", "RechargeArrived", "MemberBirthday", "MemberPackageExpiring" };

    public WeChatController(
        ApplicationDbContext db,
        ITenantContext tenantContext,
        IWeChatMiniProgramClient wechat,
        IOptions<WeChatOptions> options,
        ILogger<WeChatController> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _wechat = wechat;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// 返回面向顾客的订阅消息模板 ID。小程序拿去调 wx.requestSubscribeMessage 申请授权——
    /// 微信订阅消息是"一次性订阅"，用户每授权一次，后端才有一次下发额度，否则下发会被 43101 拒收。
    /// 模板 ID 非机密，匿名可取；未配置时返回空表，小程序据此跳过授权。
    /// </summary>
    [HttpGet("subscribe-templates")]
    public ActionResult<SubscribeTemplatesResponse> SubscribeTemplates()
    {
        var map = CustomerNotificationKinds
            .Where(k => _options.Templates.TryGetValue(k, out var t) && !string.IsNullOrWhiteSpace(t))
            .ToDictionary(k => k, k => _options.Templates[k]);
        return Ok(new SubscribeTemplatesResponse(map));
    }

    /// <summary>wx.login 的临时 code 换稳定 OpenId。小程序启动时调用，结果存本地缓存。</summary>
    [HttpPost("session")]
    public async Task<ActionResult<WeChatSessionResponse>> Session(
        [FromBody] WeChatSessionRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Code))
            return BadRequest(new { code = "InvalidInput", message = "缺少登录 code" });
        if (!_wechat.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { code = "WeChatNotConfigured", message = "服务端未配置微信小程序凭证" });

        var session = await _wechat.Code2SessionAsync(req.Code, ct);
        if (session is null)
            return BadRequest(new { code = "WeChatLoginFailed", message = "微信登录失败，请重试" });

        return Ok(new WeChatSessionResponse(session.OpenId));
    }

    /// <summary>
    /// 把当前小程序用户绑定到门店下手机号匹配的会员卡。
    /// 幂等：重复绑定同一会员只是覆盖 OpenId。
    /// </summary>
    [HttpPost("bind-member")]
    public async Task<ActionResult<BoundMemberDto>> BindMember(
        [FromBody] BindMemberRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.OpenId))
            return BadRequest(new { code = "InvalidInput", message = "缺少微信 OpenId，请重新进入小程序" });
        if (string.IsNullOrWhiteSpace(req.Phone))
            return BadRequest(new { code = "InvalidInput", message = "请填写手机号" });

        // 顾客匿名请求，无租户身份：通过 storeId 反查租户，全程 bypass。
        _tenantContext.BypassTenantFilter();

        var store = await _db.Stores.AsNoTracking()
            .Where(s => s.Id == req.StoreId && s.IsActive)
            .Select(s => new { s.TenantId })
            .FirstOrDefaultAsync(ct);
        if (store is null)
            return BadRequest(new { code = "StoreNotFound", message = "门店不存在或已停用" });

        var phone = req.Phone.Trim();
        var member = await _db.Members
            .Where(m => m.TenantId == store.TenantId && m.Phone == phone && m.IsActive)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (member is null)
            return NotFound(new { code = "MemberNotFound", message = "未找到该手机号的会员卡，请先到店开卡" });

        var openId = req.OpenId.Trim();
        if (member.WechatOpenId != openId)
        {
            member.WechatOpenId = openId;
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Member {MemberId} bound to WeChat openId", member.Id);
        }

        return Ok(new BoundMemberDto(
            member.Id, member.CardNo, member.Name, member.Balance, member.Level.ToString()));
    }
}
