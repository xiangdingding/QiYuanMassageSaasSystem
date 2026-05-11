using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 排班：某员工某日某门店的预定上岗时段。CallNext 不直接看 Schedule，
/// 但报表会用它对比"应到/实到"。
/// </summary>
public class StaffSchedule : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long StoreId { get; set; }
    public Store Store { get; set; } = null!;
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public DateOnly WorkDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 员工请假申请；批准后 Pos 端会把该日 Queue 强制置 OffDuty。
/// </summary>
public class LeaveRequest : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public LeaveType Type { get; set; } = LeaveType.Personal;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string? Reason { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public long? ApproverUserId { get; set; }
    public User? ApproverUser { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
