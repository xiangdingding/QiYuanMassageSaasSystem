using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MassageSaas.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var tenantContext = sp.GetRequiredService<ITenantContext>();
        var logger = sp.GetRequiredService<ILogger<ApplicationDbContext>>();

        tenantContext.BypassTenantFilter();

        await db.Database.MigrateAsync(ct);

        if (!await db.Plans.IgnoreQueryFilters().AnyAsync(ct))
        {
            db.Plans.AddRange(
                new Plan { Code = "basic",      Name = "基础版",    MaxStores = 1, MaxStaff = 10, AnnualPrice = 1980m, IsActive = true },
                new Plan { Code = "standard",   Name = "标准版",    MaxStores = 3, MaxStaff = 30, AnnualPrice = 4980m, IsActive = true },
                new Plan { Code = "enterprise", Name = "旗舰版",    MaxStores = 10, MaxStaff = 200, AnnualPrice = 12800m, IsActive = true });
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded default plans");
        }

        var hasPlatformAdmin = await db.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Role == UserRole.PlatformAdmin, ct);
        if (!hasPlatformAdmin)
        {
            db.Users.Add(new User
            {
                TenantId = null,
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin@123"),
                RealName = "平台管理员",
                Role = UserRole.PlatformAdmin,
                IsActive = true
            });
            await db.SaveChangesAsync(ct);
            logger.LogWarning("Seeded default platform admin: username=admin password=admin@123 — please change immediately");
        }
    }
}
