using System.IO.Ports;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MassageSaas.Cs.Services.Devices.Serial;

/// <summary>
/// 串口客显（VFD）真实驱动。主流国产 20×2 客显都兼容 ESC/POS VFD 指令子集；
/// 我们只用最小集合：0x1B 0x40 初始化、0x0C 清屏、0x0B 回到起点、0x0D/0x0A 换行。
/// 写入失败只记日志、绝不抛出——客显故障不能拖累结账主流程。
/// </summary>
public sealed class SerialCustomerDisplay : ICustomerDisplay, IDisposable
{
    private static readonly byte[] Init = { 0x1B, 0x40 };
    private static readonly byte[] ClearCmd = { 0x0C };
    private static readonly byte[] CrLf = { 0x0D, 0x0A };

    private readonly CustomerDisplaySettings _settings;
    private readonly ILogger<SerialCustomerDisplay> _logger;
    private readonly Encoding _encoding;
    private bool _welcomed;

    public SerialCustomerDisplay(AppSettings appSettings, ILogger<SerialCustomerDisplay> logger)
    {
        _settings = appSettings.CustomerDisplay;
        _logger = logger;
        _encoding = ResolveEncoding(_settings.CodePage);
    }

    public bool Enabled { get; set; } = true;

    public void ShowAmount(string label, decimal amount)
    {
        if (!Enabled) return;
        ShowText(label, $"¥{amount:F2}");
    }

    public void ShowText(string line1, string? line2 = null)
    {
        if (!Enabled) return;
        try
        {
            EnsureWelcomed();
            var buf = new List<byte>(64);
            buf.AddRange(Init);
            buf.AddRange(ClearCmd);
            buf.AddRange(_encoding.GetBytes(Truncate(line1)));
            if (!string.IsNullOrEmpty(line2))
            {
                buf.AddRange(CrLf);
                buf.AddRange(_encoding.GetBytes(Truncate(line2!)));
            }
            Send(buf.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "客显写入失败 line1={Line1} line2={Line2}", line1, line2 ?? string.Empty);
        }
    }

    public void Clear()
    {
        if (!Enabled) return;
        try
        {
            Send(Init.Concat(ClearCmd).ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "客显清屏失败");
        }
    }

    /// <summary>首次写入前发欢迎语；后续不再发，避免每次都覆盖业务文案。</summary>
    private void EnsureWelcomed()
    {
        if (_welcomed || !_settings.ShowWelcomeOnStart) return;
        _welcomed = true;
        try
        {
            var buf = new List<byte>(32);
            buf.AddRange(Init);
            buf.AddRange(ClearCmd);
            buf.AddRange(_encoding.GetBytes(Truncate(_settings.WelcomeText)));
            Send(buf.ToArray());
        }
        catch
        {
            // 欢迎语失败不重要，下次正式写文案时还会重试
        }
    }

    /// <summary>按 GBK 字节数截断到一行宽度，避免汉字断码后乱字。</summary>
    private string Truncate(string text)
    {
        var width = Math.Max(8, _settings.CharsPerLine);
        if (_encoding.GetByteCount(text) <= width) return text;

        var buf = new StringBuilder(text.Length);
        var used = 0;
        foreach (var ch in text)
        {
            var w = _encoding.GetByteCount(new[] { ch });
            if (used + w > width) break;
            buf.Append(ch);
            used += w;
        }
        return buf.ToString();
    }

    private void Send(byte[] data)
    {
        using var sp = new SerialPort(_settings.SerialPort, _settings.BaudRate, Parity.None, 8, StopBits.One)
        {
            WriteTimeout = 3000,
            Handshake = Handshake.None
        };
        sp.Open();
        sp.Write(data, 0, data.Length);
        Thread.Sleep(60);
    }

    public void Dispose() { }

    private static Encoding ResolveEncoding(string codePage)
    {
        try
        {
            return int.TryParse(codePage, out var cp)
                ? Encoding.GetEncoding(cp)
                : Encoding.GetEncoding(codePage);
        }
        catch
        {
            return Encoding.GetEncoding(936);
        }
    }
}
