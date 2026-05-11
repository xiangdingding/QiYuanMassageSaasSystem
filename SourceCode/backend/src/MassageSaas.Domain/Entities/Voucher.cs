using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 团购券（美团/大众点评等外部平台券码）+ 店内优惠券（满减/折扣）统一表。
/// 收银台核销：扫描/录入 code，校验有效性后挂到 Order.VoucherId。
/// </summary>
public class Voucher : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public VoucherKind Kind { get; set; }

    /// <summary>券码（团购券由外部平台给；店内券可由系统生成）。</summary>
    public string Code { get; set; } = null!;
    public string Title { get; set; } = null!;

    /// <summary>面值（满减场景的减额；折扣场景见 DiscountPercent）。</summary>
    public decimal FaceValue { get; set; }
    /// <summary>启用满减时，订单需达到的最小金额。</summary>
    public decimal MinOrderAmount { get; set; }
    /// <summary>折扣百分比（0.9 = 9 折），与 FaceValue 二选一。</summary>
    public decimal? DiscountPercent { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public VoucherStatus Status { get; set; } = VoucherStatus.Active;
    public DateTime? RedeemedAt { get; set; }
    public long? RedeemedOrderId { get; set; }

    /// <summary>外部平台名（如 "Meituan"/"Dianping"）；店内券为空。</summary>
    public string? Platform { get; set; }
    public string? Remark { get; set; }
}
