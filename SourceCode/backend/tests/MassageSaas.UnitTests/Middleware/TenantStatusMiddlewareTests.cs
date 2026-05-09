using FluentAssertions;
using MassageSaas.Api.Middleware;
using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.UnitTests.Middleware;

public class TenantStatusMiddlewareTests
{
    private static (ApplicationDbContext Db, TenantContext Ctx) BuildDb(long tenantId, TenantStatus status, DateTime? expireAt)
    {
        var ctx = new TenantContext { TenantId = tenantId };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"db_{Guid.NewGuid()}")
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "T",
            ContactPhone = "12345",
            Status = status,
            ExpireAt = expireAt
        });
        db.SaveChanges();
        // reset bypass for middleware to test its own bypass call
        var fresh = new TenantContext { TenantId = tenantId };
        var freshDb = new ApplicationDbContext(options, fresh);
        return (freshDb, fresh);
    }

    private static DefaultHttpContext NewHttpContext(string method, string path)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = method;
        ctx.Request.Path = path;
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    [Fact]
    public async Task GetRequest_PassesThrough_EvenWhenExpired()
    {
        var (db, ctx) = BuildDb(1, TenantStatus.Expired, DateTime.UtcNow.AddDays(-1));
        var nextCalled = false;
        var mw = new TenantStatusMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        var http = NewHttpContext("GET", "/api/orders");
        await mw.InvokeAsync(http, ctx, db);

        nextCalled.Should().BeTrue();
        http.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task WriteRequest_OnExpiredTenant_Returns403()
    {
        var (db, ctx) = BuildDb(1, TenantStatus.Expired, DateTime.UtcNow.AddDays(-1));
        var nextCalled = false;
        var mw = new TenantStatusMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        var http = NewHttpContext("POST", "/api/orders");
        await mw.InvokeAsync(http, ctx, db);

        nextCalled.Should().BeFalse();
        http.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task WriteRequest_OnExpiredDateButActiveStatus_StillBlocked()
    {
        // ExpireAt is in the past but Status=Active (race window) — middleware should still block
        var (db, ctx) = BuildDb(1, TenantStatus.Active, DateTime.UtcNow.AddMinutes(-1));
        var mw = new TenantStatusMiddleware(_ => Task.CompletedTask);

        var http = NewHttpContext("POST", "/api/orders");
        await mw.InvokeAsync(http, ctx, db);

        http.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task WriteRequest_OnActiveTenant_PassesThrough()
    {
        var (db, ctx) = BuildDb(1, TenantStatus.Active, DateTime.UtcNow.AddDays(180));
        var nextCalled = false;
        var mw = new TenantStatusMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        var http = NewHttpContext("POST", "/api/orders");
        await mw.InvokeAsync(http, ctx, db);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task WhitelistedPath_PassesThrough_EvenIfExpired()
    {
        var (db, ctx) = BuildDb(1, TenantStatus.Expired, DateTime.UtcNow.AddDays(-1));
        var nextCalled = false;
        var mw = new TenantStatusMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        var http = NewHttpContext("POST", "/api/subscriptions/pay");
        await mw.InvokeAsync(http, ctx, db);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task PlatformAdmin_BypassesCheck()
    {
        var (db, ctx) = BuildDb(1, TenantStatus.Expired, DateTime.UtcNow.AddDays(-1));
        ctx.IsPlatformAdmin = true;
        var nextCalled = false;
        var mw = new TenantStatusMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        var http = NewHttpContext("POST", "/api/tenants");
        await mw.InvokeAsync(http, ctx, db);

        nextCalled.Should().BeTrue();
    }
}
