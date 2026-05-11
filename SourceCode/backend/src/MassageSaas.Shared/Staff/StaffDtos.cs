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
    DateTime CreatedAt,
    string TechnicianLevel,
    string? BlindCertNo,
    int MaxRoundsPerDay,
    string? Specialties);

public record CreateStaffRequest(
    long StoreId,
    string Username,
    string Password,
    string Role,
    string? RealName,
    string? Phone,
    int? EmployeeNo,
    bool IsBlind = false,
    string? TechnicianLevel = null,
    string? BlindCertNo = null,
    int MaxRoundsPerDay = 0,
    string? Specialties = null);

public record UpdateStaffRequest(
    long StoreId,
    string Role,
    string? RealName,
    string? Phone,
    int? EmployeeNo,
    bool IsBlind,
    bool IsActive,
    string? TechnicianLevel = null,
    string? BlindCertNo = null,
    int MaxRoundsPerDay = 0,
    string? Specialties = null);

public record ResetPasswordRequest(string NewPassword);
