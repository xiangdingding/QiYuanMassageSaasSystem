using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows;
using MassageSaas.Shared.AppVersions;

namespace MassageSaas.Cs.Services;

/// <summary>
/// CS 端升级：读取当前版本、向后端检测最新版、下载安装包（带进度与可选 SHA256 校验）、
/// 运行安装程序并退出当前应用。检测走匿名接口，启动时即可调用。
/// </summary>
public class UpdateService
{
    private readonly IApiClient _api;

    public UpdateService(IApiClient api) => _api = api;

    /// <summary>当前应用版本（点分三段，如 1.0.0），取自程序集版本。</summary>
    public static string CurrentVersion =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";

    /// <summary>检测最新版本（8 秒超时，避免 API 不可达时拖慢启动）。</summary>
    public async Task<AppVersionCheckResult> CheckAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        return await _api.CheckAppVersionAsync("Cs", CurrentVersion, cts.Token);
    }

    /// <summary>下载安装包到临时目录，成功返回本地路径；配置了 SHA256 则校验，不匹配抛异常。</summary>
    public async Task<string> DownloadAsync(AppVersionCheckResult r, IProgress<double>? progress, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        using var resp = await http.GetAsync(r.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        var total = resp.Content.Headers.ContentLength ?? r.FileSizeBytes;
        var path = Path.Combine(Path.GetTempPath(), $"齐源按摩-setup-{r.LatestVersion}.exe");

        using (var src = await resp.Content.ReadAsStreamAsync(ct))
        using (var dst = File.Create(path))
        {
            var buffer = new byte[81920];
            long readTotal = 0;
            int n;
            while ((n = await src.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                await dst.WriteAsync(buffer, 0, n, ct);
                readTotal += n;
                if (total is > 0) progress?.Report((double)readTotal / total.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(r.Sha256))
        {
            var actual = await ComputeSha256Async(path, ct);
            if (!string.Equals(actual, r.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                TryDelete(path);
                throw new InvalidOperationException("安装包校验失败（SHA256 不匹配），已取消更新，请稍后重试。");
            }
        }

        return path;
    }

    /// <summary>启动安装程序并退出当前应用（让安装包覆盖安装）。</summary>
    public static void RunInstallerAndExit(string installerPath)
    {
        Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
        Application.Current.Shutdown();
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken ct)
    {
        using var sha = SHA256.Create();
        using var fs = File.OpenRead(path);
        var hash = await sha.ComputeHashAsync(fs, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
    }
}
