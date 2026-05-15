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
    string? Remark = null);

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
    string? MergedGroupKey);

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
    IReadOnlyList<OrderItemDto> Items);

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
    string? MemberCardNo);

public record RefundRequest(string? Reason);
