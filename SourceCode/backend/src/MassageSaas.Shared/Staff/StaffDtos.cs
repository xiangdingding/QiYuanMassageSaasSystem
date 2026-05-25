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
    string? Specialties,
    string? IdCardNo = null,
    DateTime? BirthDate = null,
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null,
    DateTime? HireDate = null,
    DateTime? TerminationDate = null);

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
    string? Specialties = null,
    string? IdCardNo = null,
    DateTime? BirthDate = null,
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null,
    DateTime? HireDate = null,
    DateTime? TerminationDate = null);

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
    string? Specialties = null,
    string? IdCardNo = null,
    DateTime? BirthDate = null,
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null,
    DateTime? HireDate = null,
    DateTime? TerminationDate = null);

public record ResetPasswordRequest(string NewPassword);

public record StaffTransferDto(
    long Id,
    long UserId,
    string UserName,
    long FromStoreId,
    string FromStoreName,
    long ToStoreId,
    string ToStoreName,
    string Kind,
    string Status,
    DateTime EffectiveFrom,
    DateTime? ExpectedReturnAt,
    DateTime? ReturnedAt,
    string? Reason,
    string? OperatorName,
    DateTime CreatedAt);

public record TransferStaffRequest(
    long ToStoreId,
    string Kind,                     // Permanent / Temporary
    DateTime? ExpectedReturnAt,
    string? Reason);
