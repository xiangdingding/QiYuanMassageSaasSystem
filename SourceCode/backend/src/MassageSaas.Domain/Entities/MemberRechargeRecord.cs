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
    public long? OperatorUserId { get; set; }
    public User? OperatorUser { get; set; }
    public string? Remark { get; set; }
}
