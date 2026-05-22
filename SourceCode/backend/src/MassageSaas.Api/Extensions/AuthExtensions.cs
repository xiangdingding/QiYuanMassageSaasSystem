using System.Text;
using MassageSaas.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MassageSaas.Api.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Missing Jwt configuration section");

        services.AddSingleton<ITokenService, TokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("PlatformAdmin", p => p.RequireRole("PlatformAdmin"));
            options.AddPolicy("ShopStaff", p => p.RequireRole("ShopOwner", "StoreManager", "Cashier"));
            // 店领导层：仅店主与店长，不含收银员。用于卡种 / 提成规则 / 工资等敏感配置。
            options.AddPolicy("ShopLeadership", p => p.RequireRole("ShopOwner", "StoreManager"));
            options.AddPolicy("Technician", p => p.RequireRole("Technician"));
        });

        return services;
    }
}
