using System.Linq;
using MassageSaas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MassageSaas.IntegrationTests;

/// <summary>
/// 集成测试用的 Web 工厂：启动真实 HTTP 管线（路由/认证/租户中间件/控制器/序列化），
/// 但把 MySQL 换成 InMemory，并关闭开机迁移与通知后台服务，使测试无需 Docker 即可运行。
/// 需要真实 MySQL 的端到端测试可另用项目里已引入的 Testcontainers.MySql 编写。
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"it-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // 提供占位连接串，避免 AddInfrastructure 因缺失而抛出
        builder.UseSetting("ConnectionStrings:MySql", "Server=localhost;Database=placeholder;Uid=test;Pwd=test;");
        builder.UseSetting("Database:RunMigrationsOnStartup", "false");
        builder.UseSetting("Notifications:Enabled", "false");

        builder.ConfigureTestServices(services =>
        {
            // 移除真实 MySQL 的 DbContext 注册，换成 InMemory
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                || d.ServiceType == typeof(DbContextOptions)
                || d.ServiceType == typeof(ApplicationDbContext)).ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}
