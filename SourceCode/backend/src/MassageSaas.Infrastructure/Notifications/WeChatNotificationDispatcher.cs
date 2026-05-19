using System.Text;
using System.Text.Json;
using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MassageSaas.Infrastructure.Notifications;

/// <summary>
/// 微信小程序订阅消息通道。把 NotificationOutbox 通过 message/subscribe/send 下发。
///
/// 模板字段：优先用 NotificationOutbox.PayloadJson（必须是订阅消息要求的
/// {"key":{"value":"..."}} 结构）；为空时回退成单字段 thing1=Body（截断到 20 字）。
/// 因为订阅消息模板字段名由商户在微信公众平台配置，无法在代码里通用化——
/// 生产端要精确控制时应写好 PayloadJson。
/// </summary>
public class WeChatNotificationDispatcher : INotificationDispatcher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWeChatAccessTokenProvider _tokenProvider;
    private readonly WeChatOptions _options;
    private readonly ILogger<WeChatNotificationDispatcher> _logger;

    public WeChatNotificationDispatcher(
        IHttpClientFactory httpClientFactory,
        IWeChatAccessTokenProvider tokenProvider,
        IOptions<WeChatOptions> options,
        ILogger<WeChatNotificationDispatcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _tokenProvider = tokenProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<NotificationDispatchResult> SendAsync(NotificationOutbox n, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(n.RecipientOpenId))
            return NotificationDispatchResult.Fail("收件人无微信 OpenId，无法下发订阅消息");

        if (!_options.Templates.TryGetValue(n.Kind.ToString(), out var templateId)
            || string.IsNullOrWhiteSpace(templateId))
            return NotificationDispatchResult.Fail($"未配置 {n.Kind} 的订阅消息模板 ID");

        var token = await _tokenProvider.GetTokenAsync(ct);
        if (string.IsNullOrWhiteSpace(token))
            return NotificationDispatchResult.Fail("获取微信 access_token 失败");

        var payload = new Dictionary<string, object?>
        {
            ["touser"] = n.RecipientOpenId,
            ["template_id"] = templateId,
            ["miniprogram_state"] = _options.MiniProgramState,
            ["lang"] = "zh_CN",
            ["data"] = BuildData(n)
        };
        if (!string.IsNullOrWhiteSpace(_options.Page))
            payload["page"] = _options.Page;

        try
        {
            var url = $"https://api.weixin.qq.com/cgi-bin/message/subscribe/send?access_token={token}";
            var client = _httpClientFactory.CreateClient("wechat");
            var body = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var resp = await client.PostAsync(url, body, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(json);
            var errcode = doc.RootElement.TryGetProperty("errcode", out var ec) ? ec.GetInt32() : -1;
            if (errcode == 0)
            {
                _logger.LogInformation("WeChat subscribe message sent kind={Kind} to={OpenId}", n.Kind, n.RecipientOpenId);
                return NotificationDispatchResult.Ok();
            }

            var errmsg = doc.RootElement.TryGetProperty("errmsg", out var em) ? em.GetString() : "unknown";
            return NotificationDispatchResult.Fail($"微信下发失败 errcode={errcode} errmsg={errmsg}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WeChat subscribe message send threw kind={Kind}", n.Kind);
            return NotificationDispatchResult.Fail($"微信下发异常：{ex.Message}");
        }
    }

    /// <summary>把 PayloadJson 解析成订阅消息 data；无则回退单字段。</summary>
    private static Dictionary<string, object> BuildData(NotificationOutbox n)
    {
        if (!string.IsNullOrWhiteSpace(n.PayloadJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(n.PayloadJson);
                if (parsed is { Count: > 0 })
                {
                    var data = new Dictionary<string, object>();
                    foreach (var (k, v) in parsed)
                    {
                        // 已是 {"value":...} 结构则原样；否则包一层
                        data[k] = v.ValueKind == JsonValueKind.Object && v.TryGetProperty("value", out _)
                            ? v
                            : new { value = v.ToString() };
                    }
                    return data;
                }
            }
            catch (JsonException)
            {
                // PayloadJson 不是合法 JSON，落到回退逻辑
            }
        }

        var text = n.Body.Length > 20 ? n.Body[..20] : n.Body;
        return new Dictionary<string, object> { ["thing1"] = new { value = text } };
    }
}
