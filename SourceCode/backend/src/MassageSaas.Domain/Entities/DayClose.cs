using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 日结/交接班。一店一日唯一（StoreId + BusinessDate 唯一索引）。
/// 提交后该日订单视为锁定：OrdersController 在写操作前必须检查 IsLocked。
/// </summary>
public class DayClose : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long StoreId { get; set; }
    public Store Store { get; set; } = null!;
    public DateOnly BusinessDate { get; set; }

    public decimal ExpectedCash { get; set; }
    public decimal ActualCash { get; set; }
    public decimal Variance { get; set; }   // = ActualCash - ExpectedCash

    public decimal RevenueTotal { get; set; }
    public decimal CashAmount { get; set; }
    public decimal MemberCardAmount { get; set; }
    public decimal WechatAmount { get; set; }
    public decimal AlipayAmount { get; set; }
    public decimal BankCardAmount { get; set; }
    public decimal RechargeAmount { get; set; }
    public int OrderCount { get; set; }

    public long? OperatorUserId { get; set; }
    public User? OperatorUser { get; set; }
    public string? Remark { get; set; }
}
