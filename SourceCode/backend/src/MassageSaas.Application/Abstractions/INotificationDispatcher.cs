using MassageSaas.Domain.Entities;

namespace MassageSaas.Application.Abstractions;

/// <summary>
/// 通知发送通道抽象。当前内置 Logging 实现仅写日志；
/// 后续接入微信公众号模板消息 / 小程序订阅消息 / 短信时新增实现，
/// 替换或扩展 DI 注册即可。
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>发送一条通知。返回成功或异常信息（异常会被调度器写到 ErrorMessage 并 RetryCount++）。</summary>
    Task<NotificationDispatchResult> SendAsync(NotificationOutbox notification, CancellationToken ct);
}

public readonly record struct NotificationDispatchResult(bool Success, string? Error)
{
    public static NotificationDispatchResult Ok() => new(true, null);
    public static NotificationDispatchResult Fail(string error) => new(false, error);
}
