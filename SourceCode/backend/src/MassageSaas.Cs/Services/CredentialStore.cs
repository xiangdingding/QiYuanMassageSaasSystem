using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MassageSaas.Cs.Services;

/// <summary>
/// “记住账号密码”凭据持久化。密码用 Windows DPAPI（CurrentUser 作用域）加密后落盘，
/// 仅当前 Windows 登录用户能解密，避免明文存储。文件位置 %AppData%/MassageSaas.Cs/credentials.json，
/// 与 session.json / prefs.json 同目录。与登录态无关，未登录也能调。
/// </summary>
public class CredentialStore
{
    private record Snapshot(string Username, string ProtectedPassword);

    private readonly string _filePath;

    public CredentialStore()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MassageSaas.Cs");
        _filePath = Path.Combine(dir, "credentials.json");
    }

    /// <summary>读出已记住的账号密码；文件缺失/损坏/无法解密时返回 null。</summary>
    public (string Username, string Password)? Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return null;
            var snap = JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(_filePath));
            if (snap is null || string.IsNullOrEmpty(snap.Username)) return null;
            var cipher = Convert.FromBase64String(snap.ProtectedPassword);
            var plain = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
            return (snap.Username, Encoding.UTF8.GetString(plain));
        }
        catch { return null; }
    }

    public void Save(string username, string password)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var cipher = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(password), null, DataProtectionScope.CurrentUser);
            var json = JsonSerializer.Serialize(new Snapshot(username, Convert.ToBase64String(cipher)));
            File.WriteAllText(_filePath, json);
        }
        catch { /* 写盘失败不影响登录；下次仍需手动输入 */ }
    }

    public void Clear()
    {
        try { if (File.Exists(_filePath)) File.Delete(_filePath); }
        catch { /* ignore */ }
    }
}
