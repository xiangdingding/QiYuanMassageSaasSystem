using Microsoft.Extensions.Logging;

namespace MassageSaas.Cs.Services.Devices;

/// <summary>一通来电。</summary>
public record IncomingCall(string PhoneNumber, DateTime At);

/// <summary>
/// 来电显示监听器（来电盒通常走串口/Modem，识别到主叫号码后抛事件）。
/// 收银端订阅 <see cref="CallReceived"/>，可据来电号码语音播报、弹出会员资料。
/// 当前为占位实现 <see cref="NullCallerIdMonitor"/>，永不抛事件；接真实来电盒后只换 DI 注册。
/// </summary>
public interface ICallerIdMonitor
{
    /// <summary>识别到来电时触发。</summary>
    event EventHandler<IncomingCall>? CallReceived;

    /// <summary>开始监听（应用启动后调用）。</summary>
    void Start();

    /// <summary>停止监听（应用退出时调用）。</summary>
    void Stop();
}

/// <summary>占位来电监听：不连接任何硬件，永不抛事件。无来电盒的门店一直用它。</summary>
public sealed class NullCallerIdMonitor : ICallerIdMonitor
{
    private readonly ILogger<NullCallerIdMonitor> _logger;

    public NullCallerIdMonitor(ILogger<NullCallerIdMonitor> logger) => _logger = logger;

    // 占位实现不抛事件；CS0067 在此是预期的。
#pragma warning disable CS0067
    public event EventHandler<IncomingCall>? CallReceived;
#pragma warning restore CS0067

    public void Start() => _logger.LogInformation("来电监听未接硬件（占位）");

    public void Stop() { }
}
