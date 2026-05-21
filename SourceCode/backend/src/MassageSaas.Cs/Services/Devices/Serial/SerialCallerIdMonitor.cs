using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace MassageSaas.Cs.Services.Devices.Serial;

/// <summary>
/// 串口来电盒真实监听。开机后后台线程一直读串口，看到 NMBR=xxx 行就抛 CallReceived。
/// 兼容主流厂商的 ASCII Caller-ID 输出（USR / 联通来电小秘 / 国产 56k modem 等）：
///   RING\r\n
///   DATE = 0102
///   TIME = 0903
///   NMBR = 13800138000
/// 读串口失败会指数退避重连，不会停掉监听。
/// </summary>
public sealed class SerialCallerIdMonitor : ICallerIdMonitor, IDisposable
{
    private static readonly Regex NumberLine = new(@"NMBR\s*=\s*([0-9\-+]+)", RegexOptions.IgnoreCase);

    private readonly CallerIdSettings _settings;
    private readonly ILogger<SerialCallerIdMonitor> _logger;
    private CancellationTokenSource? _cts;
    private Thread? _worker;

    public SerialCallerIdMonitor(AppSettings appSettings, ILogger<SerialCallerIdMonitor> logger)
    {
        _settings = appSettings.CallerId;
        _logger = logger;
    }

    public event EventHandler<IncomingCall>? CallReceived;

    public void Start()
    {
        if (_worker is not null) return;
        _cts = new CancellationTokenSource();
        _worker = new Thread(() => Loop(_cts.Token))
        {
            IsBackground = true,
            Name = "CallerIdMonitor"
        };
        _worker.Start();
        _logger.LogInformation("来电监听已启动 port={Port} baud={Baud}",
            _settings.SerialPort, _settings.BaudRate);
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }
        _worker?.Join(TimeSpan.FromSeconds(2));
        _worker = null;
        _cts?.Dispose();
        _cts = null;
    }

    private void Loop(CancellationToken token)
    {
        var backoffSeconds = 2;
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var sp = new SerialPort(_settings.SerialPort, _settings.BaudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 1000,
                    Encoding = Encoding.ASCII,
                    NewLine = "\n",
                    Handshake = Handshake.None
                };
                sp.Open();
                backoffSeconds = 2;

                while (!token.IsCancellationRequested && sp.IsOpen)
                {
                    string? line;
                    try { line = sp.ReadLine(); }
                    catch (TimeoutException) { continue; }

                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var m = NumberLine.Match(line);
                    if (!m.Success) continue;

                    var number = NormalizeNumber(m.Groups[1].Value);
                    if (number.Length == 0) continue;
                    try
                    {
                        CallReceived?.Invoke(this, new IncomingCall(number, DateTime.Now));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "来电事件处理器抛异常 number={Number}", number);
                    }
                }
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "来电串口异常，{Backoff}s 后重试 port={Port}",
                    backoffSeconds, _settings.SerialPort);
                try { Task.Delay(TimeSpan.FromSeconds(backoffSeconds), token).Wait(token); }
                catch { /* cancellation */ }
                backoffSeconds = Math.Min(backoffSeconds * 2, 30);
            }
        }
    }

    private static string NormalizeNumber(string raw)
    {
        var buf = new StringBuilder(raw.Length);
        foreach (var ch in raw)
        {
            if (ch is >= '0' and <= '9') buf.Append(ch);
        }
        return buf.ToString();
    }

    public void Dispose() => Stop();
}
