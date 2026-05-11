namespace MassageSaas.Shared.MemberPackages;

public record MemberPackageDto(
    long Id,
    long MemberId,
    string MemberName,
    long StoreId,
    string Kind,            // Counter / Period
    long? ServiceId,
    string? ServiceName,
    string Title,
    decimal PaidAmount,
    int TotalCount,
    int RemainCount,
    DateTime? ValidFrom,
    DateTime? ExpiresAt,
    string Status,
    string? Remark,
    DateTime CreatedAt);

public record CreateMemberPackageRequest(
    long MemberId,
    long StoreId,
    string Kind,
    long? ServiceId,
    string Title,
    decimal PaidAmount,
    int TotalCount,
    DateTime? ValidFrom,
    DateTime? ExpiresAt,
    string? Remark);
