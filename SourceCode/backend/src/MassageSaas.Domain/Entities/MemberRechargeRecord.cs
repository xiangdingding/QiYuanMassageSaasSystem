using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

public class MemberRechargeRecord : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public long StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public decimal Amount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal BalanceAfter { get; set; }

    public PayMethod PayMethod { get; set; } = PayMethod.Cash;
    /// <summary>流水类型。区分充值/退卡/转赠/返佣，便于审计与报表过滤。</summary>
    public MemberRechargeKind Kind { get; set; } = MemberRechargeKind.Recharge;
    /// <summary>转赠 / 返佣时的对手会员；其他场景为空。</summary>
    public long? CounterpartyMemberId { get; set; }
    public Member? CounterpartyMember { get; set; }
    public long? OperatorUserId { get; set; }
    public User? OperatorUser { get; set; }
    public string? Remark { get; set; }
}
