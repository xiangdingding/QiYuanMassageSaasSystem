namespace MassageSaas.Cs.Services.Devices;

/// <summary>
/// 来电盒（caller-id box）配置。Enabled=false（默认）时仍走占位监听器，永不触发事件。
/// 绝大多数国产来电盒（联通来电小秘、56k modem 等）走 RS232 + ASCII，振铃时输出
/// 类似  RING\r\nNMBR=13800138000\r\n 的文本。
/// </summary>
public class CallerIdSettings
{
    public bool Enabled { get; set; }

    public string SerialPort { get; set; } = "COM3";
    public int BaudRate { get; set; } = 1200;
}
