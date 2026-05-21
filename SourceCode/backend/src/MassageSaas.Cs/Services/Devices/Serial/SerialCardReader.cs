using System.IO.Ports;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MassageSaas.Cs.Services.Devices.Serial;

/// <summary>
/// 串口磁条读卡器真实监听。读串口收到一行 Track2 数据后剥掉哨兵 ; / = / ?，
/// 拿到的纯数字即会员卡卡号，抛 CardSwiped 让 MainViewModel 自动调出会员。
/// 标准 Track2：;CARDNUMBER=DISCRETIONARY?LRC\r\n
/// 简化磁条卡：CARDNUMBER\r\n
/// 失败指数退避重连，不停监听。
/// </summary>
public sealed class SerialCardReader : ICardReader, IDisposable
{
    private readonly CardReaderSettings _settings;
    private readonly ILogger<SerialCardReader> _logger;
    private CancellationTokenSource? _cts;
    private Thread? _worker;

    public SerialCardReader(AppSettings appSettings, ILogger<SerialCardReader> logger)
    {
        _settings = appSettings.CardReader;
        _logger = logger;
    }

    public event EventHandler<CardSwipe>? CardSwiped;

    public void Start()
    {
        if (_worker is not null) return;
        _cts = new CancellationTokenSource();
        _worker = new Thread(() => Loop(_cts.Token))
        {
            IsBackground = true,
            Name = "CardReader"
        };
        _worker.Start();
        _logger.LogInformation("磁条读卡器已启动 port={Port} baud={Baud}",
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
                    NewLine = "\r",
                    Handshake = Handshake.None
                };
                sp.Open();
                backoffSeconds = 2;

                while (!token.IsCancellationRequested && sp.IsOpen)
                {
                    string? line;
                    try { line = sp.ReadLine(); }
                    catch (TimeoutException) { continue; }

                    var number = ExtractCardNumber(line);
                    if (string.IsNullOrEmpty(number)) continue;
                    try
                    {
                        CardSwiped?.Invoke(this, new CardSwipe(number, DateTime.Now));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "刷卡事件处理器抛异常 card={Card}", number);
                    }
                }
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "磁条读卡器串口异常，{Backoff}s 后重试 port={Port}",
                    backoffSeconds, _settings.SerialPort);
                try { Task.Delay(TimeSpan.FromSeconds(backoffSeconds), token).Wait(token); }
                catch { /* cancellation */ }
                backoffSeconds = Math.Min(backoffSeconds * 2, 30);
            }
        }
    }

    /// <summary>
    /// 提取卡号：Track2 标准是 ;CARD=DISC?LRC；分割符前段就是卡号。
    /// 没有哨兵的简化数据流则取全部 ASCII 数字字符。
    /// </summary>
    internal static string ExtractCardNumber(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var line = raw.Trim();

        var start = line.IndexOf(';');
        if (start >= 0)
        {
            var end = line.IndexOfAny(new[] { '=', '?' }, start + 1);
            line = end > start ? line.Substring(start + 1, end - start - 1) : line[(start + 1)..];
        }

        var buf = new StringBuilder(line.Length);
        foreach (var ch in line)
        {
            if (ch is >= '0' and <= '9') buf.Append(ch);
        }
        return buf.ToString();
    }

    public void Dispose() => Stop();
}
