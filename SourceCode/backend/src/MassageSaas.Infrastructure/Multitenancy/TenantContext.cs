using MassageSaas.Application.Abstractions;

namespace MassageSaas.Infrastructure.Multitenancy;

public class TenantContext : ITenantContext
{
    public long? TenantId { get; set; }
    public bool IsPlatformAdmin { get; set; }
    public long? UserId { get; set; }
    public bool IsFilterBypassed { get; private set; }

    public void BypassTenantFilter() => IsFilterBypassed = true;
}
