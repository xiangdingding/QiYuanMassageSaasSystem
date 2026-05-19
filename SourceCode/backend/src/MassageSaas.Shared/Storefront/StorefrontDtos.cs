namespace MassageSaas.Shared.Storefront;

/// <summary>顾客小程序看到的服务项（不含成本/提成等内部字段）。</summary>
public record StorefrontServiceDto(
    long Id,
    string Name,
    int DurationMinutes,
    decimal Price,
    decimal MemberPrice,
    string? Description);

/// <summary>顾客小程序看到的技师（用于预约时指定）。</summary>
public record StorefrontTechnicianDto(
    long Id,
    string Name,
    string Level,
    bool IsBlind,
    string? Specialties);

/// <summary>顾客小程序看到的会员套餐概要。</summary>
public record StorefrontPackageDto(
    long Id,
    string Title,
    string Kind,
    string? ServiceName,
    int RemainCount,
    int TotalCount,
    DateTime? ExpiresAt,
    string Status);

/// <summary>顾客小程序"我的"页：会员卡实时概要 + 在用套餐。</summary>
public record StorefrontMemberDto(
    long MemberId,
    string CardNo,
    string? Name,
    decimal Balance,
    string Level,
    IReadOnlyList<StorefrontPackageDto> Packages);
