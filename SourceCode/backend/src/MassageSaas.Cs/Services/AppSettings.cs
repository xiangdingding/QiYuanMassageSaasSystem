using System.IO;
using System.Text.Json;
using MassageSaas.Cs.Services.Devices;

namespace MassageSaas.Cs.Services;

public class AppSettings
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5000/api";

    public string TokenFilePath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassageSaas.Cs", "session.json");

    /// <summary>小票打印机配置。</summary>
    public PrinterSettings Printer { get; set; } = new();

    /// <summary>客显（VFD 小屏）配置。</summary>
    public CustomerDisplaySettings CustomerDisplay { get; set; } = new();

    /// <summary>来电盒配置。</summary>
    public CallerIdSettings CallerId { get; set; } = new();

    /// <summary>磁条读卡器配置。</summary>
    public CardReaderSettings CardReader { get; set; } = new();

    /// <summary>
    /// 从 exe 同目录的 appsettings.json 加载配置；文件缺失或损坏时回退到默认值。
    /// 这样接打印机/改后端地址只需改 json，不必重新编译。
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path)) return new AppSettings();

            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return loaded ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }
}
