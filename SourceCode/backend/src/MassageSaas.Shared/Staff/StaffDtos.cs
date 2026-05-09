namespace MassageSaas.Shared.Staff;

public record StaffDto(
    long Id,
    long? StoreId,
    string Username,
    string? RealName,
    string? Phone,
    string Role,
    int? EmployeeNo,
    bool IsBlind,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public record CreateStaffRequest(
    long StoreId,
    string Username,
    string Password,
    string Role,
    string? RealName,
    string? Phone,
    int? EmployeeNo,
    bool IsBlind = false);

public record UpdateStaffRequest(
    long StoreId,
    string Role,
    string? RealName,
    string? Phone,
    int? EmployeeNo,
    bool IsBlind,
    bool IsActive);

public record ResetPasswordRequest(string NewPassword);
