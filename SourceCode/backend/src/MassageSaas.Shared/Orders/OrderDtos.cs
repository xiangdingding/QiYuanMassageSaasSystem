namespace MassageSaas.Shared.Orders;

public record OrderItemInputDto(
    long ServiceId,
    long TechnicianId,
    int Quantity = 1,
    long? RoomId = null);

public record CreateOrderRequest(
    long StoreId,
    long? MemberId,
    IReadOnlyList<OrderItemInputDto> Items,
    string? Remark);

public record CheckoutRequest(
    string PayMethod,
    decimal? PaidAmount,
    decimal DiscountAmount = 0m,
    string? Remark = null,
    /// <summary>会员卡合并结算：在 Order.Member 之外再依次扣这些卡的余额；仅 PayMethod=MemberCard 生效。</summary>
    IReadOnlyList<long>? SecondaryMemberIds = null);

public record OrderItemDto(
    long Id,
    long ServiceId,
    string ServiceName,
    long TechnicianId,
    string? TechnicianName,
    int Quantity,
    int DurationMinutes,
    decimal UnitPrice,
    decimal ItemTotal,
    decimal CommissionAmount,
    long? RoomId,
    string? RoomNo,
    long? PreviousTechnicianId,
    DateTime? TransferredAt,
    decimal TipAmount,
    long? MemberPackageId,
    bool IsAddOn,
    string? MergedGroupKey,
    /// <summary>下单时记录的服务面值单价（即使本项走了次卡核销也保留）。</summary>
    decimal ListUnitPrice = 0m,
    /// <summary>面值小计 = ListUnitPrice × Quantity，仅用于小票展示。</summary>
    decimal ListAmount = 0m);

public record TransferTechnicianRequest(long NewTechnicianId, string? Reason);

/// <summary>并钟：把多个未结账订单项标记为"同一技师同时服务"。需 ≥ 2 项且同技师。</summary>
public record MergeOrderItemsRequest(IReadOnlyList<long> OrderItemIds);

public record AddOrderItemsRequest(IReadOnlyList<OrderItemInputDto> Items);

public record SetTipRequest(decimal TipAmount);

public record ReopenOrderRequest(string? Reason);

public record OrderDto(
    long Id,
    string OrderNo,
    long StoreId,
    long? MemberId,
    string? MemberCardNo,
    decimal Total,
    decimal DiscountAmount,
    decimal PaidAmount,
    decimal TipAmount,
    string PayMethod,
    string Status,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    long? CashierUserId,
    string? CashierName,
    string? Remark,
    long? VoucherId,
    DateTime? ReopenedAt,
    string? ReopenReason,
    IReadOnlyList<OrderItemDto> Items,
    /// <summary>面值合计 = Σ Item.ListAmount；与 Total（现金可收金额）独立，次卡订单也有非 0 值。</summary>
    decimal ListTotal = 0m,
    /// <summary>本单走次卡核销的总次数 = Σ Item.Quantity (MemberPackageId != null)。</summary>
    int PunchCardUsedCount = 0,
    /// <summary>会员手机（无会员或非会员卡支付场景为 null）。</summary>
    string? MemberPhone = null,
    /// <summary>会员姓名（无会员则 null）。</summary>
    string? MemberName = null,
    /// <summary>会员卡类型展示名（如"金卡/100次足疗卡"）；非会员订单 null。</summary>
    string? MemberTypeName = null,
    /// <summary>会员卡类型枚举字符串：StoredValue/CountBased；非会员订单 null。</summary>
    string? MemberTypeKind = null);

public record OrderListItemDto(
    long Id,
    string OrderNo,
    decimal Total,
    decimal PaidAmount,
    string PayMethod,
    string Status,
    int ItemCount,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? MemberCardNo,
    /// <summary>会员手机号；非会员订单为 null。</summary>
    string? MemberPhone = null);

public record RefundRequest(string? Reason);
