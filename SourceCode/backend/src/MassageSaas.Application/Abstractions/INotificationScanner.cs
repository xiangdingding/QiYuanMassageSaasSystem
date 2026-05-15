namespace MassageSaas.Application.Abstractions;

public interface INotificationScanner
{
    /// <summary>扫描所有触发条件并写入 NotificationOutbox（幂等）。返回新增条数。</summary>
    Task<int> ScanAndEnqueueAsync(CancellationToken ct);
}
