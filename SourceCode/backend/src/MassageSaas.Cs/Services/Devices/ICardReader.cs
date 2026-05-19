using Microsoft.Extensions.Logging;

namespace MassageSaas.Cs.Services.Devices;

/// <summary>一次磁条会员卡刷卡，CardNumber 即卡号。</summary>
public record CardSwipe(string CardNumber, DateTime At);

/// <summary>
/// 磁条会员卡读卡器。刷卡后抛出卡号，收银端据此自动调出会员。
/// 当前为占位实现 <see cref="NullCardReader"/>，永不抛事件；接真实读卡器（串口/HID）后只换 DI 注册。
/// </summary>
public interface ICardReader
{
    /// <summary>刷卡识别到卡号时触发。</summary>
    event EventHandler<CardSwipe>? CardSwiped;

    /// <summary>开始监听（应用启动后调用）。</summary>
    void Start();

    /// <summary>停止监听（应用退出时调用）。</summary>
    void Stop();
}

/// <summary>占位读卡器：不连接任何硬件，永不抛事件。无读卡器的门店一直用它。</summary>
public sealed class NullCardReader : ICardReader
{
    private readonly ILogger<NullCardReader> _logger;

    public NullCardReader(ILogger<NullCardReader> logger) => _logger = logger;

    // 占位实现不抛事件；CS0067 在此是预期的。
#pragma warning disable CS0067
    public event EventHandler<CardSwipe>? CardSwiped;
#pragma warning restore CS0067

    public void Start() => _logger.LogInformation("磁条读卡器未接硬件（占位）");

    public void Stop() { }
}
