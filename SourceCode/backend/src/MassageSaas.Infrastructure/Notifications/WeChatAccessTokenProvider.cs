using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MassageSaas.Infrastructure.Notifications;

/// <summary>微信 access_token 提供器。带进程内缓存（默认 2 小时有效，提前 5 分钟刷新）。</summary>
public interface IWeChatAccessTokenProvider
{
    Task<string?> GetTokenAsync(CancellationToken ct);
}

/// <summary>
/// 单例：缓存 access_token，并发请求用 SemaphoreSlim 串行化，避免重复拉取把微信调用次数耗光。
/// </summary>
public class WeChatAccessTokenProvider : IWeChatAccessTokenProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WeChatOptions _options;
    private readonly ILogger<WeChatAccessTokenProvider> _logger;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _cachedToken;
    private DateTime _expiresAtUtc = DateTime.MinValue;

    public WeChatAccessTokenProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<WeChatOptions> options,
        ILogger<WeChatAccessTokenProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> GetTokenAsync(CancellationToken ct)
    {
        if (_cachedToken is not null && DateTime.UtcNow < _expiresAtUtc)
            return _cachedToken;

        await _lock.WaitAsync(ct);
        try
        {
            // 双检：可能已被其它线程刷新
            if (_cachedToken is not null && DateTime.UtcNow < _expiresAtUtc)
                return _cachedToken;

            var url = "https://api.weixin.qq.com/cgi-bin/token"
                      + $"?grant_type=client_credential&appid={Uri.EscapeDataString(_options.AppId)}"
                      + $"&secret={Uri.EscapeDataString(_options.AppSecret)}";

            var client = _httpClientFactory.CreateClient("wechat");
            using var resp = await client.GetAsync(url, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("access_token", out var tokenEl))
            {
                var expiresIn = root.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 7200;
                _cachedToken = tokenEl.GetString();
                // 提前 5 分钟过期，留刷新余量
                _expiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, expiresIn - 300));
                return _cachedToken;
            }

            var errcode = root.TryGetProperty("errcode", out var ec) ? ec.GetInt32() : -1;
            var errmsg = root.TryGetProperty("errmsg", out var em) ? em.GetString() : "unknown";
            _logger.LogError("WeChat access_token fetch failed errcode={Code} errmsg={Msg}", errcode, errmsg);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WeChat access_token fetch threw");
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }
}
