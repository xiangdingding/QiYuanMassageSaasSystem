namespace MassageSaas.Shared.Vouchers;

public record VoucherDto(
    long Id,
    string Kind,            // GroupBuy / StoreCoupon
    string Code,
    string Title,
    decimal FaceValue,
    decimal MinOrderAmount,
    decimal? DiscountPercent,
    DateTime? ValidFrom,
    DateTime? ExpiresAt,
    string Status,
    string? Platform,
    string? Remark,
    DateTime? RedeemedAt,
    long? RedeemedOrderId,
    DateTime CreatedAt);

public record CreateVoucherRequest(
    string Kind,
    string Code,
    string Title,
    decimal FaceValue,
    decimal MinOrderAmount,
    decimal? DiscountPercent,
    DateTime? ValidFrom,
    DateTime? ExpiresAt,
    string? Platform,
    string? Remark);

public record VoucherRedeemRequest(string Code, long OrderId);
