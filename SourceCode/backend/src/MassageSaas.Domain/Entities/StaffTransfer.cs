using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 员工跨店调动记录。连锁门店常见：技师从 A 店永久调动到 B 店，或临时借调（旺季支援）。
/// 创建时立即把 User.StoreId 切到目标店并重置其叫号队列；临时借调可经 Return 归还。
/// </summary>
public class StaffTransfer : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }

    public long UserId { get; set; }
    public User User { get; set; } = null!;

    public long FromStoreId { get; set; }
    public Store FromStore { get; set; } = null!;
    public long ToStoreId { get; set; }
    public Store ToStore { get; set; } = null!;

    public StaffTransferKind Kind { get; set; }
    public StaffTransferStatus Status { get; set; } = StaffTransferStatus.InEffect;

    public DateTime EffectiveFrom { get; set; }
    /// <summary>临时借调的预计归还时间；永久调动为空。</summary>
    public DateTime? ExpectedReturnAt { get; set; }
    public DateTime? ReturnedAt { get; set; }

    public string? Reason { get; set; }
    public long? OperatorUserId { get; set; }
    public User? OperatorUser { get; set; }
}
