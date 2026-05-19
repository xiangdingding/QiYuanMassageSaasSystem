using System.Text;
using Microsoft.Extensions.Logging;

namespace MassageSaas.Cs.Services.Devices.EscPos;

/// <summary>
/// 真实 ESC/POS 热敏小票打印机驱动。支持串口与网口连接，钱箱经打印机 RJ11 口踢开。
/// 打印失败只记日志、绝不抛出——结账主流程不能被打印机故障打断。
/// </summary>
public sealed class EscPosReceiptPrinter : IReceiptPrinter
{
    private readonly PrinterSettings _settings;
    private readonly ILogger<EscPosReceiptPrinter> _logger;
    private readonly Encoding _encoding;

    public EscPosReceiptPrinter(AppSettings appSettings, ILogger<EscPosReceiptPrinter> logger)
    {
        _settings = appSettings.Printer;
        _logger = logger;
        _encoding = ResolveEncoding(_settings.CodePage);
    }

    public bool Enabled { get; set; } = true;

    public void Print(ReceiptDocument document)
    {
        if (!Enabled) return;
        try
        {
            var writer = new EscPosWriter(_encoding, _settings.CharsPerLine);
            CreateTransport().Send(writer.BuildReceipt(document, _settings.CutPaper));
            _logger.LogInformation("已打印小票 order={OrderNo}", document.OrderNo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "小票打印失败 order={OrderNo}", document.OrderNo);
        }
    }

    public void OpenCashDrawer()
    {
        if (!Enabled || !_settings.OpenDrawerOnCash) return;
        try
        {
            var writer = new EscPosWriter(_encoding, _settings.CharsPerLine);
            CreateTransport().Send(writer.BuildDrawerKick());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "钱箱踢出失败");
        }
    }

    public PrinterTestResult SelfTest(string storeName)
    {
        try
        {
            var writer = new EscPosWriter(_encoding, _settings.CharsPerLine);
            var bytes = writer.BuildReceipt(ReceiptDocument.CreateTestSample(storeName), _settings.CutPaper)
                .Concat(writer.BuildDrawerKick())
                .ToArray();
            CreateTransport().Send(bytes);
            var via = _settings.IsNetwork
                ? $"网口 {_settings.Host}:{_settings.Port}"
                : $"串口 {_settings.SerialPort} @ {_settings.BaudRate}";
            return new PrinterTestResult(true, $"测试小票已发送（{via}）。请检查出纸、切纸与钱箱。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试打印失败");
            return new PrinterTestResult(false, $"测试打印失败：{ex.Message}");
        }
    }

    private IReceiptTransport CreateTransport() =>
        _settings.IsNetwork
            ? new NetworkReceiptTransport(_settings.Host, _settings.Port)
            : new SerialReceiptTransport(_settings.SerialPort, _settings.BaudRate);

    /// <summary>解析编码名/代码页；失败兜底 GBK(936)。需先注册 CodePagesEncodingProvider。</summary>
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
