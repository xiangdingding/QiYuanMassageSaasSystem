using System.Text.Json;
using MassageSaas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MassageSaas.Api.HealthChecks;

/// <summary>数据库连通性健康检查：探测 ApplicationDbContext 能否连上库。</summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _db;

    public DatabaseHealthCheck(ApplicationDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            return await _db.Database.CanConnectAsync(ct)
                ? HealthCheckResult.Healthy("数据库连接正常")
                : HealthCheckResult.Unhealthy("数据库无法连接");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("数据库检查异常", ex);
        }
    }
}

/// <summary>把健康检查结果写成 JSON：整体状态 + 各检查项明细，便于运维监控抓取。</summary>
public static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = new
        {
            status = report.Status.ToString(),
            time = DateTime.UtcNow,
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1)
            })
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
