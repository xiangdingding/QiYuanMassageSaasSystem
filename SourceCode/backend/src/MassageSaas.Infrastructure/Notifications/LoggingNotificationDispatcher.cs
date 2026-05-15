using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MassageSaas.Infrastructure.Notifications;

/// <summary>
/// 默认 dispatcher：把通知写到日志即视为发送成功。
/// 上线对接微信/短信前一直是这一份；之后用真实通道替换 DI 注册。
/// </summary>
public class LoggingNotificationDispatcher : INotificationDispatcher
{
    private readonly ILogger<LoggingNotificationDispatcher> _logger;

    public LoggingNotificationDispatcher(ILogger<LoggingNotificationDispatcher> logger)
    {
        _logger = logger;
    }

    public Task<NotificationDispatchResult> SendAsync(NotificationOutbox n, CancellationToken ct)
    {
        _logger.LogInformation(
            "[Notification] kind={Kind} tenant={Tenant} dedup={Dedup} title=\"{Title}\" body=\"{Body}\" phone={Phone} openId={OpenId}",
            n.Kind, n.TenantId, n.DedupKey, n.Title, n.Body, n.RecipientPhone, n.RecipientOpenId);
        return Task.FromResult(NotificationDispatchResult.Ok());
    }
}
