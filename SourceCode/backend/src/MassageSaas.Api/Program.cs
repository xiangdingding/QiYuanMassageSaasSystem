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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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
