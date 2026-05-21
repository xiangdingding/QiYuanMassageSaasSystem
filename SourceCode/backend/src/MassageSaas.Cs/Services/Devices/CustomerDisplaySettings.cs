namespace MassageSaas.Cs.Services.Devices;

/// <summary>
/// 客显（顾客端 VFD 小屏）配置。Enabled=false（默认）时仍走占位实现，门店没装客显也不报错。
/// 主流国产客显（CD-5220 / Posiflex / Epson DM-D 等）都兼容 ESC/POS VFD 指令子集。
/// </summary>
public class CustomerDisplaySettings
{
    public bool Enabled { get; set; }

    public string SerialPort { get; set; } = "COM2";
    public int BaudRate { get; set; } = 9600;

    /// <summary>中文编码。国产 VFD 与小票打印机同样基本是 GBK。</summary>
    public string CodePage { get; set; } = "GBK";

    /// <summary>单行字符数：20×2 屏是 20；ASCII 1 列，汉字 2 列。</summary>
    public int CharsPerLine { get; set; } = 20;

    /// <summary>启动后首次写入前是否清屏并显示欢迎语。</summary>
    public bool ShowWelcomeOnStart { get; set; } = true;

    /// <summary>欢迎语（仅 ShowWelcomeOnStart=true 时使用）。</summary>
    public string WelcomeText { get; set; } = "欢迎光临";
}
