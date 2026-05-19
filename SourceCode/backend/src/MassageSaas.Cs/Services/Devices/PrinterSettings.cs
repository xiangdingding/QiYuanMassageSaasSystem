namespace MassageSaas.Cs.Services.Devices;

/// <summary>
/// 小票打印机连接配置。从 exe 同目录的 appsettings.json 的 "Printer" 节加载。
/// Enabled=false（默认）时仍走占位打印机，门店没装打印机也不报错。
/// </summary>
public class PrinterSettings
{
    public bool Enabled { get; set; }

    /// <summary>连接方式：Serial（串口）或 Network（网口 TCP，端口通常 9100）。</summary>
    public string Connection { get; set; } = "Serial";

    public string SerialPort { get; set; } = "COM1";
    public int BaudRate { get; set; } = 9600;

    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 9100;

    /// <summary>中文编码。国产 ESC/POS 打印机基本是 GBK；可改 GB18030。</summary>
    public string CodePage { get; set; } = "GBK";

    /// <summary>单行字符数：58mm 纸约 32，80mm 纸约 48。</summary>
    public int CharsPerLine { get; set; } = 32;

    /// <summary>打印完是否切纸。</summary>
    public bool CutPaper { get; set; } = true;

    /// <summary>现金结账时是否踢钱箱。</summary>
    public bool OpenDrawerOnCash { get; set; } = true;

    public bool IsNetwork =>
        string.Equals(Connection, "Network", StringComparison.OrdinalIgnoreCase);
}
