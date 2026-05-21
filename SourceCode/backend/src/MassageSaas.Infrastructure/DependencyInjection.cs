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

        // 微信集成：HttpClient 与小程序登录客户端始终注册——顾客绑定会员卡（code2Session）
        // 即使没开订阅消息也要能走通。
        var wechatSection = configuration.GetSection(WeChatOptions.SectionName);
        services.Configure<WeChatOptions>(wechatSection);
        var wechat = wechatSection.Get<WeChatOptions>() ?? new WeChatOptions();
        services.AddHttpClient("wechat");
        services.AddScoped<IWeChatMiniProgramClient, WeChatMiniProgramClient>();

        // 通知通道：配置了微信且 Enabled=true 走微信订阅消息，否则走日志通道（占位）
        if (wechat.IsConfigured)
        {
            services.AddSingleton<IWeChatAccessTokenProvider, WeChatAccessTokenProvider>();
            services.AddScoped<INotificationDispatcher, WeChatNotificationDispatcher>();
        }
        else
        {
            services.AddScoped<INotificationDispatcher, LoggingNotificationDispatcher>();
        }

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
                    // 不开 EnableRetryOnFailure：项目里多处控制器（Orders / Members / Tenants /
                    // Subscriptions / Inventory / Vouchers / Callback）手动 BeginTransactionAsync，
                    // 与 MySqlRetryingExecutionStrategy 互斥，会抛
                    // "user-initiated transactions are not supported"。如要恢复重试，得把每个手动事务
                    // 包进 Database.CreateExecutionStrategy().ExecuteAsync 里。
                    mysql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name);
                });
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }
}
