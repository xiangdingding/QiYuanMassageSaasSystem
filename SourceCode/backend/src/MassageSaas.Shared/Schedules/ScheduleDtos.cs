namespace MassageSaas.Shared.Schedules;

public record StaffScheduleDto(
    long Id,
    long StoreId,
    long UserId,
    string UserName,
    DateTime WorkDate,
    string StartTime,        // "09:00"
    string EndTime,
    string? Remark);

public record CreateStaffScheduleRequest(
    long StoreId,
    long UserId,
    DateTime WorkDate,
    string StartTime,
    string EndTime,
    string? Remark);

public record LeaveRequestDto(
    long Id,
    long UserId,
    string UserName,
    string Type,
    DateTime FromDate,
    DateTime ToDate,
    string? Reason,
    string Status,
    long? ApproverUserId,
    string? ApproverName,
    DateTime? ApprovedAt,
    DateTime CreatedAt);

public record CreateLeaveRequest(
    long UserId,
    string Type,
    DateTime FromDate,
    DateTime ToDate,
    string? Reason);

public record ApproveLeaveRequest(bool Approve, string? Reason);
