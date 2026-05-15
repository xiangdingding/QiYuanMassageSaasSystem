using MassageSaas.Application.Abstractions;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Notifications;
using MassageSaas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MassageSaas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<INotificationScanner, NotificationScanner>();
        services.AddScoped<INotificationDispatcher, LoggingNotificationDispatcher>();
        if (configuration.GetValue("Notifications:Enabled", true))
        {
            services.AddHostedService<NotificationDispatchService>();
        }

        var connectionString = configuration.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("Missing connection string 'MySql'");

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseMySql(
                connectionString,
                serverVersion,
                mysql =>
                {
                    mysql.EnableRetryOnFailure(3);
                    mysql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name);
                });
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }
}
