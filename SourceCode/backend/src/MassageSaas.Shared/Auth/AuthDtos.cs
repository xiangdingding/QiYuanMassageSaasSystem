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

public record UserProfileDto(
    long Id,
    string Username,
    string? RealName,
    string? Phone,
    string Role,
    long? TenantId,
    long? StoreId);

public record UpdateProfileRequest(string? RealName, string? Phone);

public record ChangePasswordRequest(string OldPassword, string NewPassword);
