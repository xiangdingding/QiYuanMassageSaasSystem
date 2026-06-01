using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MassageSaas.Cs.Services.Devices;

/// <summary>小票上的一行消费明细。Amount 是面值小计；PaidViaPunchCard=true 表示该项次卡核销，现金未收。</summary>
public record ReceiptLine(string ServiceName, int Quantity, string TechnicianName, decimal Amount, bool PaidViaPunchCard = false);

/// <summary>一张结账小票的结构化内容，由打印机驱动负责排版。</summary>
public record ReceiptDocument(
    string StoreName,
    string OrderNo,
    DateTime PrintedAt,
    IReadOnlyList<ReceiptLine> Items,
    decimal Total,
    decimal Discount,
    decimal Paid,
    decimal Change,
    string PayMethod,
    /// <summary>本单走次卡核销的总次数；&gt;0 时小票多打一行"消费次数"。</summary>
    int PunchCardUsedCount = 0)
{
    /// <summary>构造一张测试小票，覆盖多明细/优惠/找零，供"测试打印"现场调试用。</summary>
    public static ReceiptDocument CreateTestSample(string storeName) => new(
        StoreName: string.IsNullOrWhiteSpace(storeName) ? "测试小票" : storeName,
        OrderNo: $"TEST-{DateTime.Now:HHmmss}",
        PrintedAt: DateTime.Now,
        Items: new[]
        {
            new ReceiptLine("全身按摩 60 分钟", 1, "测试技师", 198m),
            new ReceiptLine("足疗 30 分钟", 1, "测试技师", 88m)
        },
        Total: 286m,
        Discount: 6m,
        Paid: 300m,
        Change: 20m,
        PayMethod: "现金");
}

/// <summary>测试打印结果，用于在界面上反馈成功或失败原因。</summary>
public record PrinterTestResult(bool Success, string Message);

/// <summary>
/// 小票打印机 + 钱箱。钱箱通常挂在打印机的 RJ11 接口上，由打印机踢开，故合并到一个抽象里。
/// 当前为占位实现 <see cref="LoggingReceiptPrinter"/>；接入真实热敏打印机（如 ESC/POS 串口/USB）后
/// 只需替换 DI 注册，调用方代码不变。
/// </summary>
public interface IReceiptPrinter
{
    /// <summary>用户在设置里关闭后整个组件静默（无打印机的门店不报错）。</summary>
    bool Enabled { get; set; }

    /// <summary>打印一张结账小票。</summary>
    void Print(ReceiptDocument document);

    /// <summary>踢开钱箱（现金结账时调用）。</summary>
    void OpenCashDrawer();

    /// <summary>打印一张测试小票并踢一次钱箱，返回结果。现场调试打印机时用。</summary>
    PrinterTestResult SelfTest(string storeName);
}

/// <summary>
/// 占位打印机：把小票与钱箱动作写入日志，不驱动真实硬件。
/// 没有打印机的门店可一直用它；有打印机时换成 ESC/POS 实现。
/// </summary>
public sealed class LoggingReceiptPrinter : IReceiptPrinter
{
    private readonly ILogger<LoggingReceiptPrinter> _logger;

    public LoggingReceiptPrinter(ILogger<LoggingReceiptPrinter> logger) => _logger = logger;

    public bool Enabled { get; set; } = true;

    public void Print(ReceiptDocument document)
    {
        if (!Enabled) return;
        _logger.LogInformation("打印小票（占位）\n{Receipt}", Format(document));
    }

    public void OpenCashDrawer()
    {
        if (!Enabled) return;
        _logger.LogInformation("踢开钱箱（占位）");
    }

    public PrinterTestResult SelfTest(string storeName)
    {
        Print(ReceiptDocument.CreateTestSample(storeName));
        OpenCashDrawer();
        return new PrinterTestResult(true,
            "当前为占位打印机（未配置真实打印机）。测试小票内容已写入日志，未实际出纸。");
    }

    /// <summary>把小票排成纯文本，便于占位阶段核对内容。</summary>
    private static string Format(ReceiptDocument d)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"      {d.StoreName}");
        sb.AppendLine($"订单：{d.OrderNo}");
        sb.AppendLine($"时间：{d.PrintedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("--------------------------------");
        foreach (var i in d.Items)
        {
            var tag = i.PaidViaPunchCard ? "[次卡]" : "";
            sb.AppendLine($"{i.ServiceName} x{i.Quantity}次  {i.TechnicianName}  ¥{i.Amount:F2} {tag}".TrimEnd());
        }
        sb.AppendLine("--------------------------------");
        if (d.Discount > 0) sb.AppendLine($"优惠：¥{d.Discount:F2}");
        sb.AppendLine($"合计：¥{d.Total:F2}");
        sb.AppendLine($"实收：¥{d.Paid:F2}（{d.PayMethod}）");
        if (d.PunchCardUsedCount > 0) sb.AppendLine($"消费次数：{d.PunchCardUsedCount} 次（次卡核销）");
        if (d.Change > 0) sb.AppendLine($"找零：¥{d.Change:F2}");
        return sb.ToString();
    }
}
