using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 月度工资单批次：一个门店一个月一条；UNIQUE(StoreId, Year, Month)。
/// Draft 可修改/重算；Locked 后只读不能再加项；Paid 表示已发放。
/// </summary>
public class PayrollPeriod : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public int Year { get; set; }
    public int Month { get; set; }
    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;

    public DateTime GeneratedAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public long? OperatorUserId { get; set; }
    public User? OperatorUser { get; set; }

    public decimal TotalAmount { get; set; }
    public string? Remark { get; set; }

    public ICollection<PayrollItem> Items { get; set; } = new List<PayrollItem>();
}

/// <summary>
/// 个人月度工资明细：净额 = 底薪 + 提成 + 加班 + 满勤 + 调整合计（小费仅做汇总展示，不进净额）。
/// </summary>
public class PayrollItem : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long PeriodId { get; set; }
    public PayrollPeriod Period { get; set; } = null!;
    public long UserId { get; set; }
    public User User { get; set; } = null!;

    public decimal BaseSalary { get; set; }
    public decimal CommissionTotal { get; set; }
    /// <summary>本月小费合计（不计入 NetTotal，由技师从客人手里直接拿；这里只汇总便于核对）。</summary>
    public decimal TipsTotal { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal OvertimeAmount { get; set; }
    public decimal AttendanceBonus { get; set; }
    public decimal AdjustmentTotal { get; set; }
    public decimal NetTotal { get; set; }

    public int ServedRoundCount { get; set; }
    public int ScheduledDays { get; set; }
    public int LeaveDays { get; set; }

    public string? Remark { get; set; }

    public ICollection<PayrollAdjustment> Adjustments { get; set; } = new List<PayrollAdjustment>();
}

/// <summary>
/// 工资单上的一次性调整：奖金（加项）或扣款（减项）。
/// </summary>
public class PayrollAdjustment : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long ItemId { get; set; }
    public PayrollItem Item { get; set; } = null!;
    public PayrollAdjustmentKind Kind { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = null!;
    public long? OperatorUserId { get; set; }
    public User? OperatorUser { get; set; }
}
