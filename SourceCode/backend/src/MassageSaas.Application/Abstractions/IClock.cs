namespace MassageSaas.Application.Abstractions;

/// <summary>系统时钟抽象，便于扫描器/调度器测试注入伪造时间。</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
