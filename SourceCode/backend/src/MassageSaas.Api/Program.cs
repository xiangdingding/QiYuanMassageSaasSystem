using MassageSaas.Api.Extensions;
using MassageSaas.Api.HealthChecks;
using MassageSaas.Api.Json;
using MassageSaas.Api.Middleware;
using MassageSaas.Infrastructure;
using MassageSaas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, config) => config
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/massage-saas-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // 全系统时间统一北京时间：存 UTC，API 边界转 UTC+8
        o.JsonSerializerOptions.Converters.Add(new BeijingDateTimeConverter());
        o.JsonSerializerOptions.Converters.Add(new BeijingNullableDateTimeConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MassageSaas API",
        Version = "v1",
        Description = "盲人按摩门店 SaaS 系统 API"
    });

    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Bearer token",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
            Id = "Bearer",
            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuth(builder.Configuration);
builder.Services.AddHttpContextAccessor();

// CORS：移动端 App（Capacitor webview，origin 为 http(s)://localhost 或 capacitor://localhost）
// 与跨机访问的 BS 端都是跨域请求。鉴权走 Authorization: Bearer（不依赖 Cookie），
// 因此用 AllowAnyOrigin 即可，无需 AllowCredentials。
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db" });

var app = builder.Build();

if (app.Configuration.GetValue("Database:RunMigrationsOnStartup", true))
{
    try
    {
        await DbInitializer.InitializeAsync(app.Services);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database initialization failed; continuing startup");
    }
}

app.UseSerilogRequestLogging();

// Swagger：开发环境默认开启；生产（IIS）可通过 Swagger:Enabled=true 打开，
// 方便部署后用浏览器分别验证 http / https 两个地址是否都通。
if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// IIS 部署下同一站点同时绑定 http 与 https，两种协议都要可访问：
// shop-admin（B/S 租户端）走 http、移动端 mobile / CS 端走 https。
// 因此默认【不】强制跳转 https——一旦强制，http 请求会被 307 重定向，shop-admin 即不可用。
// 局域网联调同理（移动端经 http://IP 直连会撞自签证书）。
// 如确需全站强制 https，把配置 Https:ForceRedirect 设为 true 即可。
if (app.Configuration.GetValue("Https:ForceRedirect", false))
{
    app.UseHttpsRedirection();
}

app.UseCors();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();
app.UseMiddleware<TenantStatusMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthResponseWriter.WriteAsync
}).AllowAnonymous();

app.Run();

public partial class Program { }
