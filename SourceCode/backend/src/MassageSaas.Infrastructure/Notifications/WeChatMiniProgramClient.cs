using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MassageSaas.Infrastructure.Notifications;

/// <summary>微信小程序 code2Session 调用结果。</summary>
public record WeChatSession(string OpenId, string? UnionId);

/// <summary>
/// 微信小程序登录客户端。把 wx.login 得到的临时 code 换成稳定的 OpenId。
/// 只依赖 AppId/AppSecret，不依赖通知开关（WeChatOptions.Enabled）——
/// 顾客绑定会员卡这条路即使没开订阅消息也要能走通。
/// </summary>
public interface IWeChatMiniProgramClient
{
    /// <summary>用 wx.login 的 code 换 OpenId；失败返回 null。</summary>
    Task<WeChatSession?> Code2SessionAsync(string code, CancellationToken ct);

    /// <summary>是否已配置 AppId/AppSecret。未配置时 Code2Session 必然失败。</summary>
    bool IsConfigured { get; }
}

public class WeChatMiniProgramClient : IWeChatMiniProgramClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WeChatOptions _options;
    private readonly ILogger<WeChatMiniProgramClient> _logger;

    public WeChatMiniProgramClient(
        IHttpClientFactory httpClientFactory,
        IOptions<WeChatOptions> options,
        ILogger<WeChatMiniProgramClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.HasCredentials;

    public async Task<WeChatSession?> Code2SessionAsync(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;
        if (!_options.HasCredentials)
        {
            _logger.LogWarning("WeChat code2Session called but AppId/AppSecret not configured");
            return null;
        }

        try
        {
            var url = "https://api.weixin.qq.com/sns/jscode2session"
                      + $"?appid={Uri.EscapeDataString(_options.AppId)}"
                      + $"&secret={Uri.EscapeDataString(_options.AppSecret)}"
                      + $"&js_code={Uri.EscapeDataString(code)}"
                      + "&grant_type=authorization_code";

            var client = _httpClientFactory.CreateClient("wechat");
            using var resp = await client.GetAsync(url, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("openid", out var openIdEl)
                && openIdEl.GetString() is { Length: > 0 } openId)
            {
                var unionId = root.TryGetProperty("unionid", out var u) ? u.GetString() : null;
                return new WeChatSession(openId, unionId);
            }

            var errcode = root.TryGetProperty("errcode", out var ec) ? ec.GetInt32() : -1;
            var errmsg = root.TryGetProperty("errmsg", out var em) ? em.GetString() : "unknown";
            _logger.LogError("WeChat code2Session failed errcode={Code} errmsg={Msg}", errcode, errmsg);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WeChat code2Session threw");
            return null;
        }
    }
}
