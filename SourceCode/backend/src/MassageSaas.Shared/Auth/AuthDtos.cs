namespace MassageSaas.Shared.Auth;

public record LoginRequest(string Username, string Password, string? TenantCode);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User);

public record UserInfo(
    long Id,
    string Username,
    string? RealName,
    string Role,
    long? TenantId,
    long? StoreId,
    bool IsBlind);

public record RefreshRequest(string RefreshToken);
