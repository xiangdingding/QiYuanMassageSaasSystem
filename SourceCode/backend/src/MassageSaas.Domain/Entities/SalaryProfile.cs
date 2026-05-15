using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 员工薪资配置：每个 User 至多一条。月度生成工资单时从这里读底薪 / 加班时薪 / 满勤奖。
/// 没有配置的员工按 0 底薪、无加班、无满勤；提成仍按 CommissionRule 计算。
/// </summary>
public class SalaryProfile : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>每月底薪。</summary>
    public decimal BaseMonthly { get; set; }
    /// <summary>加班时薪。生成工资单时按 PayrollItem.OvertimeHours 乘以该值。</summary>
    public decimal OvertimeHourRate { get; set; }
    /// <summary>满勤奖额度（达到 RequiredAttendanceDays 后给）。</summary>
    public decimal AttendanceBonusAmount { get; set; }
    /// <summary>满勤所需的本月排班天数。0 = 不发满勤。</summary>
    public int RequiredAttendanceDays { get; set; }
    public string? Remark { get; set; }
}
