using System.IO;
using System.Text.Json;
using MassageSaas.Shared.Auth;

namespace MassageSaas.Cs.Services;

public class SessionService
{
    private readonly AppSettings _settings;
    private SessionState _state = new();

    public SessionService(AppSettings settings)
    {
        _settings = settings;
        TryLoadFromDisk();
    }

    public string? AccessToken => _state.AccessToken;
    public UserInfo? User => _state.User;
    public DateTime? ExpiresAt => _state.ExpiresAt;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_state.AccessToken);
    public long? TenantId => _state.User?.TenantId;
    public long? StoreId => _state.User?.StoreId;
    public string? Role => _state.User?.Role;

    public event Action? Changed;

    public void SignIn(LoginResponse resp)
    {
        _state = new SessionState
        {
            AccessToken = resp.AccessToken,
            ExpiresAt = resp.ExpiresAt,
            User = resp.User
        };
        SaveToDisk();
        Changed?.Invoke();
    }

    public void SignOut()
    {
        _state = new SessionState();
        try { if (File.Exists(_settings.TokenFilePath)) File.Delete(_settings.TokenFilePath); }
        catch { /* ignore */ }
        Changed?.Invoke();
    }

    private void TryLoadFromDisk()
    {
        try
        {
            if (!File.Exists(_settings.TokenFilePath)) return;
            var json = File.ReadAllText(_settings.TokenFilePath);
            var parsed = JsonSerializer.Deserialize<SessionState>(json);
            if (parsed?.AccessToken is null) return;
            if (parsed.ExpiresAt.HasValue && parsed.ExpiresAt.Value <= DateTime.UtcNow) return;
            _state = parsed;
        }
        catch { /* corrupted token file - ignore */ }
    }

    private void SaveToDisk()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settings.TokenFilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_state);
            File.WriteAllText(_settings.TokenFilePath, json);
        }
        catch { /* best-effort */ }
    }

    private class SessionState
    {
        public string? AccessToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public UserInfo? User { get; set; }
    }
}
