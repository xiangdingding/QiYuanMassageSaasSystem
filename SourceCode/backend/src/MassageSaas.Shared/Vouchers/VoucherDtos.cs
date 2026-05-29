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

/// <summary>
/// 批量生成同规格券：N 张码由服务端随机生成（SC-XXXX-XXXX / GB-XXXX-XXXX）。
/// FaceValue 与 DiscountPercent 与单建一致，二选一。
/// </summary>
public record BatchCreateVoucherRequest(
    string Kind,
    int Count,
    string Title,
    decimal FaceValue,
    decimal MinOrderAmount,
    decimal? DiscountPercent,
    DateTime? ValidFrom,
    DateTime? ExpiresAt,
    string? Platform,
    string? Remark);

public record BatchCreateVoucherResponse(int Created, IReadOnlyList<string> Codes);

/// <summary>批量作废 / 批量删除共用入参：一组券 id。</summary>
public record BulkVoucherActionRequest(IReadOnlyList<long> Ids);

/// <summary>批量操作结果：Affected = 实际改动条数；Skipped = 因状态不符跳过的明细。</summary>
public record BulkVoucherActionResponse(
    int Affected,
    IReadOnlyList<BulkVoucherSkip> Skipped);

public record BulkVoucherSkip(long Id, string? Code, string Status, string Reason);
