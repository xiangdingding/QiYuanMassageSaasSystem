namespace MassageSaas.Application.Abstractions;

public interface ITenantContext
{
    long? TenantId { get; }
    bool IsPlatformAdmin { get; }
    long? UserId { get; }
    void BypassTenantFilter();
    bool IsFilterBypassed { get; }
}

public interface ICurrentUser
{
    long? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    long? TenantId { get; }
    long? StoreId { get; }
    bool IsAuthenticated { get; }
}
