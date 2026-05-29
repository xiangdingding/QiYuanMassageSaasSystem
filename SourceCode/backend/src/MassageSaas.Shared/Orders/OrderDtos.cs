namespace MassageSaas.Shared.Orders;

public record OrderItemInputDto(
    long ServiceId,
    long TechnicianId,
    int Quantity = 1,
    long? RoomId = null,
    /// <summary>技师来源："Rotation"=轮钟（叫号）/ "Designation"=点钟（客人指定）。
    /// 不传或无效时后端兜底为 Designation —— 老客户端（CS POS）走手动选人，本就是点钟，
    /// 这样不会把指定率虚高。</summary>
    string? AssignmentSource = null);

public record CreateOrderRequest(
    long StoreId,
    long? MemberId,
    IReadOnlyList<OrderItemInputDto> Items,
    string? Remark,
    /// <summary>本次结账一起带入的进行中计时房 session id 列表；可与 Items 共存或单独存在（即纯房费单）。</summary>
    IReadOnlyList<long>? RoomSessionIds = null);

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
    decimal ListAmount = 0m,
    /// <summary>技师指派来源：Rotation 轮钟 / Designation 点钟 / Unknown 历史数据。</summary>
    string AssignmentSource = "Unknown");

public record TransferTechnicianRequest(long NewTechnicianId, string? Reason);

/// <summary>并钟：把多个未结账订单项标记为"同一技师同时服务"。需 ≥ 2 项且同技师。</summary>
public record MergeOrderItemsRequest(IReadOnlyList<long> OrderItemIds);

public record AddOrderItemsRequest(IReadOnlyList<OrderItemInputDto> Items);

public record SetTipRequest(decimal TipAmount);

public record ReopenOrderRequest(string? Reason);

/// <summary>订单里挂载的计时房费明细。与 OrderItem 并列，单独列出而非走 OrderItem，
/// 避免把"服务+技师+提成"模型污染（房费没有技师、不计提成）。</summary>
public record OrderRoomChargeDto(
    long SessionId,
    long RoomId,
    string RoomNo,
    int Minutes,
    decimal HourlyRate,
    decimal Amount,
    string PayMethod,
    string Status);

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
    /// <summary>面值合计 = Σ Item.ListAmount + Σ RoomCharges.Amount；与 Total 独立，次卡订单也有非 0 值。</summary>
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
    string? MemberTypeKind = null,
    /// <summary>挂在该订单上的计时房费明细（与 OrderItem 并列展示），不计提成。</summary>
    IReadOnlyList<OrderRoomChargeDto>? RoomCharges = null);

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

/// <summary>用于"按技师 + 日期"列出已完成订单项，供投诉登记 UI 选择。</summary>
public record TechnicianServedItemDto(
    long ItemId,
    long OrderId,
    string OrderNo,
    long ServiceId,
    string ServiceName,
    DateTime? CompletedAt,
    decimal Amount,
    long? MemberId,
    string? MemberName,
    string? MemberCardNo,
    bool HasPendingComplaint);
