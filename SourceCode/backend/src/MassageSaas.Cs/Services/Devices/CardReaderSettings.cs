namespace MassageSaas.Cs.Services.Devices;

/// <summary>
/// 磁条读卡器配置。Enabled=false（默认）时仍走占位读卡器，永不触发事件。
/// 串口读卡器刷卡时输出标准 Track2 数据 ;CARD=DISC?\r\n；
/// 我们只取 ; 与 = 之间的卡号，剥掉起止哨兵与可分自由数据。
/// </summary>
public class CardReaderSettings
{
    public bool Enabled { get; set; }

    public string SerialPort { get; set; } = "COM4";
    public int BaudRate { get; set; } = 9600;
}
