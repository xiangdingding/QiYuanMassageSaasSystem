using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MassageSaas.Cs.Services;

/// <summary>
/// 【临时调试】把 /api/auth/login 的原始响应（状态码 + Content-Type + JSON 正文）写到
/// %TEMP%\massage_login_resp.txt，用于排查 LoginResponse.User 为 null 的真因。
/// 定位后请移除本处理器与其注册。
/// </summary>
public sealed class LoginDebugHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var resp = await base.SendAsync(request, ct);
        if (request.RequestUri?.AbsolutePath.Contains("auth/login", StringComparison.OrdinalIgnoreCase) == true
            && resp.Content is not null)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            var contentType = resp.Content.Headers.ContentType;
            try
            {
                var path = Path.Combine(Path.GetTempPath(), "massage_login_resp.txt");
                File.WriteAllText(path,
                    $"Status={(int)resp.StatusCode} {resp.StatusCode}\r\nContent-Type={contentType}\r\n\r\n{body}");
            }
            catch { /* 调试用，写失败忽略 */ }
            // 重建 Content，保证 Refit 仍能正常读取反序列化
            resp.Content = new StringContent(body, Encoding.UTF8);
            if (contentType is not null) resp.Content.Headers.ContentType = contentType;
        }
        return resp;
    }
}
