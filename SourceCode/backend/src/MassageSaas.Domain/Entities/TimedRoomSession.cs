using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 计时房使用记录。客人进计时房开始计时，离开时按停留分钟数 × 房间小时单价结算。
/// 与服务订单（Order）平行的一条收入流；结算后由报表/日结按 SettledAt 归集。
/// </summary>
public class TimedRoomSession : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public long RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public long? MemberId { get; set; }
    public Member? Member { get; set; }
    /// <summary>散客姓名（无会员时填）。</summary>
    public string? CustomerName { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    /// <summary>结算时记录的房间小时单价快照（房间改价不影响历史）。</summary>
    public decimal HourlyRateSnapshot { get; set; }
    /// <summary>计费分钟数（结算时按实际时长向上取整到分钟，至少 1）。</summary>
    public int BilledMinutes { get; set; }
    public decimal Amount { get; set; }
    public PayMethod PayMethod { get; set; } = PayMethod.Unpaid;

    public TimedRoomSessionStatus Status { get; set; } = TimedRoomSessionStatus.Open;
    public long? OperatorUserId { get; set; }
    public User? OperatorUser { get; set; }
    public string? Remark { get; set; }

    /// <summary>关联订单：从收银台一起结算时记下，让计时房费成为该订单的一行。
    /// 没有关联订单（如直接在房间页 Stop 走旧接口）时为 null，仍按独立流水入账。</summary>
    public long? OrderId { get; set; }
    public Order? Order { get; set; }
}
