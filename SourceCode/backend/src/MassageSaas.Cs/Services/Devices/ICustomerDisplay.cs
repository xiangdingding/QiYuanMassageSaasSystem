using Microsoft.Extensions.Logging;

namespace MassageSaas.Cs.Services.Devices;

/// <summary>
/// 客显（面向顾客的小屏，常见为 VFD 串口屏）。展示应付/实收金额，让顾客核对。
/// 当前为占位实现 <see cref="LoggingCustomerDisplay"/>；接入真实串口屏后只换 DI 注册。
/// </summary>
public interface ICustomerDisplay
{
    bool Enabled { get; set; }

    /// <summary>显示一笔金额，带文字标签（如"应付"/"实收"）。</summary>
    void ShowAmount(string label, decimal amount);

    /// <summary>显示两行自定义文本（第二行可空）。</summary>
    void ShowText(string line1, string? line2 = null);

    /// <summary>清屏（回到欢迎语或空屏）。</summary>
    void Clear();
}

/// <summary>占位客显：把要显示的内容写入日志，不驱动真实串口屏。</summary>
public sealed class LoggingCustomerDisplay : ICustomerDisplay
{
    private readonly ILogger<LoggingCustomerDisplay> _logger;

    public LoggingCustomerDisplay(ILogger<LoggingCustomerDisplay> logger) => _logger = logger;

    public bool Enabled { get; set; } = true;

    public void ShowAmount(string label, decimal amount)
    {
        if (!Enabled) return;
        _logger.LogInformation("客显（占位）：{Label} ¥{Amount:F2}", label, amount);
    }

    public void ShowText(string line1, string? line2 = null)
    {
        if (!Enabled) return;
        _logger.LogInformation("客显（占位）：{Line1} | {Line2}", line1, line2 ?? string.Empty);
    }

    public void Clear()
    {
        if (!Enabled) return;
        _logger.LogInformation("客显（占位）：清屏");
    }
}
