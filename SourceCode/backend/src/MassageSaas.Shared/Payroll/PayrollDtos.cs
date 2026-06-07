namespace MassageSaas.Shared.Payroll;

public record SalaryProfileDto(
    long UserId,
    string UserName,
    decimal BaseMonthly,
    decimal OvertimeHourRate,
    decimal AttendanceBonusAmount,
    int RequiredAttendanceDays,
    string? Remark);

public record UpsertSalaryProfileRequest(
    decimal BaseMonthly,
    decimal OvertimeHourRate,
    decimal AttendanceBonusAmount,
    int RequiredAttendanceDays,
    string? Remark);

public record PayrollAdjustmentDto(
    long Id,
    string Kind,         // Bonus / Deduction
    decimal Amount,
    string Reason,
    string? OperatorName,
    DateTime CreatedAt);

public record AddAdjustmentRequest(string Kind, decimal Amount, string Reason);

public record PayrollItemDto(
    long Id,
    long UserId,
    string UserName,
    int? EmployeeNo,
    decimal BaseSalary,
    decimal CommissionTotal,
    decimal ReferralCommissionTotal,
    decimal TipsTotal,
    decimal OvertimeHours,
    decimal OvertimeAmount,
    decimal AttendanceBonus,
    decimal AdjustmentTotal,
    decimal NetTotal,
    int ServedRoundCount,
    int ScheduledDays,
    int LeaveDays,
    string? Remark,
    IReadOnlyList<PayrollAdjustmentDto> Adjustments);

public record UpdatePayrollItemRequest(
    decimal OvertimeHours,
    decimal AttendanceBonusOverride,    // < 0 表示沿用配置自动计算
    string? Remark);

public record PayrollPeriodDto(
    long Id,
    long StoreId,
    int Year,
    int Month,
    string Status,
    DateTime GeneratedAt,
    DateTime? LockedAt,
    DateTime? PaidAt,
    string? OperatorName,
    decimal TotalAmount,
    int ItemCount,
    string? Remark);

public record PayrollPeriodDetailDto(
    PayrollPeriodDto Period,
    IReadOnlyList<PayrollItemDto> Items);

public record GeneratePayrollRequest(long StoreId, int Year, int Month, string? Remark);

public record LockPayrollRequest(string? Remark);
