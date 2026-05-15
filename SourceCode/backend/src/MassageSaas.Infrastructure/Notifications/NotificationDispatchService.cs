using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MassageSaas.Infrastructure.Notifications;

/// <summary>
/// 后台周期任务：每隔 IntervalSeconds 跑一次扫描器入箱、再排空 Pending 通知。
/// 间隔配置：Notifications:IntervalSeconds（默认 300 秒）；
/// 单批排空数量上限：Notifications:BatchSize（默认 100）；
/// 失败重试上限：Notifications:MaxRetries（默认 5）。
/// </summary>
public class NotificationDispatchService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationDispatchService> _logger;
    private readonly TimeSpan _interval;
    private readonly int _batchSize;
    private readonly int _maxRetries;

    public NotificationDispatchService(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<NotificationDispatchService> logger)
    {
        _services = services;
        _logger = logger;
        var seconds = configuration.GetValue("Notifications:IntervalSeconds", 300);
        _interval = TimeSpan.FromSeconds(Math.Max(seconds, 30));
        _batchSize = Math.Clamp(configuration.GetValue("Notifications:BatchSize", 100), 1, 1000);
        _maxRetries = Math.Clamp(configuration.GetValue("Notifications:MaxRetries", 5), 1, 20);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "NotificationDispatchService started; interval={Interval}s batch={Batch} maxRetries={Retries}",
            _interval.TotalSeconds, _batchSize, _maxRetries);

        // 启动后立刻跑一次，避免要等一个完整周期
        await TickOnce(stoppingToken);

        using var timer = new PeriodicTimer(_interval);
        while (!stoppingToken.IsCancellationRequested
               && await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await TickOnce(stoppingToken);
        }
    }

    private async Task TickOnce(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var scanner = scope.ServiceProvider.GetRequiredService<INotificationScanner>();
            await scanner.ScanAndEnqueueAsync(ct);

            await DrainPendingAsync(scope.ServiceProvider, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification tick failed");
        }
    }

    private async Task DrainPendingAsync(IServiceProvider scopedServices, CancellationToken ct)
    {
        var db = scopedServices.GetRequiredService<ApplicationDbContext>();
        var tenantContext = scopedServices.GetRequiredService<ITenantContext>();
        var dispatcher = scopedServices.GetRequiredService<INotificationDispatcher>();
        var clock = scopedServices.GetRequiredService<IClock>();

        tenantContext.BypassTenantFilter();
        var now = clock.UtcNow;

        var batch = await db.NotificationOutbox
            .Where(n => n.Status == NotificationStatus.Pending && n.ScheduledAt <= now)
            .OrderBy(n => n.ScheduledAt)
            .Take(_batchSize)
            .ToListAsync(ct);

        foreach (var n in batch)
        {
            try
            {
                var result = await dispatcher.SendAsync(n, ct);
                if (result.Success)
                {
                    n.Status = NotificationStatus.Sent;
                    n.SentAt = clock.UtcNow;
                    n.ErrorMessage = null;
                }
                else
                {
                    HandleFailure(n, result.Error ?? "unknown");
                }
            }
            catch (Exception ex)
            {
                HandleFailure(n, ex.Message);
            }
        }

        if (batch.Count > 0) await db.SaveChangesAsync(ct);
    }

    private void HandleFailure(NotificationOutbox n, string error)
    {
        n.RetryCount++;
        n.ErrorMessage = error.Length > 500 ? error[..500] : error;
        if (n.RetryCount >= _maxRetries)
        {
            n.Status = NotificationStatus.Failed;
        }
    }
}
