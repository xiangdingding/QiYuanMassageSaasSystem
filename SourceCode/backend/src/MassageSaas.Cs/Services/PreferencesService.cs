using System.IO;
using System.Text.Json;

namespace MassageSaas.Cs.Services;

/// <summary>
/// 用户界面偏好的持久化（与登录态无关，无登录也能调）。
/// 现在只承载"正常模式 / 无障碍模式"切换；后续需要更多 UI 偏好可往这里加字段。
/// 存到 %AppData%/MassageSaas.Cs/prefs.json，与 session.json 同目录。
/// </summary>
public class PreferencesService
{
    public enum AppMode { Normal, Accessible }

    private record Snapshot(string A11yMode);

    private readonly string _filePath;
    private AppMode _a11yMode;

    public PreferencesService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MassageSaas.Cs");
        _filePath = Path.Combine(dir, "prefs.json");
        _a11yMode = Load();
    }

    /// <summary>切换后通知订阅方（App.xaml.cs 据此热加载/卸载 A11yTheme.xaml）。</summary>
    public event Action<AppMode>? Changed;

    public AppMode A11yMode
    {
        get => _a11yMode;
        set
        {
            if (_a11yMode == value) return;
            _a11yMode = value;
            Save();
            Changed?.Invoke(value);
        }
    }

    public bool IsAccessible => _a11yMode == AppMode.Accessible;

    public void Toggle() => A11yMode = IsAccessible ? AppMode.Normal : AppMode.Accessible;

    private AppMode Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return AppMode.Normal;
            var json = File.ReadAllText(_filePath);
            var snap = JsonSerializer.Deserialize<Snapshot>(json);
            return string.Equals(snap?.A11yMode, "Accessible", StringComparison.OrdinalIgnoreCase)
                ? AppMode.Accessible : AppMode.Normal;
        }
        catch { return AppMode.Normal; }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var json = JsonSerializer.Serialize(new Snapshot(_a11yMode.ToString()));
            File.WriteAllText(_filePath, json);
        }
        catch { /* 写盘失败不影响 UI；下次启动会回退默认 */ }
    }
}
