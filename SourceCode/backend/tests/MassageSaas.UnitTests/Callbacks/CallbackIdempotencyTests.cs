using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Subscriptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace MassageSaas.UnitTests.Callbacks;

public class CallbackIdempotencyTests
{
    private static (ApplicationDbContext Db, TenantContext Ctx, Tenant Tenant, Plan Plan, PaymentOrder Order)
        Seed(decimal amount = 1980m)
    {
        var ctx = new TenantContext();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"cb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();

        var tenant = new Tenant { Id = 1, Name = "店一", ContactPhone = "13800000000", Status = TenantStatus.Expired };
        var plan = new Plan { Id = 1, Code = "basic", Name = "基础版", AnnualPrice = amount, MaxStores = 1, MaxStaff = 10, IsActive = true };
        var order = new PaymentOrder
        {
            Id = 100, TenantId = tenant.Id, OrderNo = "SUB202605090001",
            Amount = amount, Channel = PaymentChannel.Wechat,
            Status = PaymentStatus.Pending, PlanId = plan.Id, Years = 1
        };
        db.Tenants.Add(tenant);
        db.Plans.Add(plan);
        db.PaymentOrders.Add(order);
        db.SaveChanges();
        return (db, ctx, tenant, plan, order);
    }

    private static CallbackController NewController(ApplicationDbContext db, TenantContext ctx) =>
        new(db, ctx, NullLogger<CallbackController>.Instance);

    [Fact]
    public async Task Simulate_paid_callback_renews_tenant_and_creates_subscription()
    {
        var (db, ctx, tenant, plan, order) = Seed();
        var c = NewController(db, ctx);

        var payload = new PaymentCallbackPayload(order.OrderNo, "WX-TX-1", order.Amount, true, "Wechat", "{}");
        var ack = await c.Simulate(payload, new FakeEnv("Development"), default);
        var ok = ack.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ((CallbackAck)ok!.Value!).Ok.Should().BeTrue();

        var saved = await db.PaymentOrders.FindAsync(order.Id);
        saved!.Status.Should().Be(PaymentStatus.Paid);
        saved.ThirdPartyTransactionNo.Should().Be("WX-TX-1");

        var sub = await db.Subscriptions.FirstAsync();
        sub.PaymentOrderId.Should().Be(order.Id);
        sub.Source.Should().Be(SubscriptionSource.OnlinePayment);

        var t = await db.Tenants.FindAsync(tenant.Id);
        t!.Status.Should().Be(TenantStatus.Active);
        t.CurrentPlanId.Should().Be(plan.Id);
        t.ExpireAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Replay_paid_callback_does_not_double_renew()
    {
        var (db, ctx, tenant, plan, order) = Seed();
        var c = NewController(db, ctx);

        var payload = new PaymentCallbackPayload(order.OrderNo, "WX-TX-2", order.Amount, true, "Wechat", "{}");
        await c.Simulate(payload, new FakeEnv("Development"), default);
        var firstExpire = (await db.Tenants.FindAsync(tenant.Id))!.ExpireAt;

        // 第二次同样的回调（重放）
        await c.Simulate(payload, new FakeEnv("Development"), default);
        var subs = await db.Subscriptions.CountAsync();
        var secondExpire = (await db.Tenants.FindAsync(tenant.Id))!.ExpireAt;

        subs.Should().Be(1);
        secondExpire.Should().Be(firstExpire);
    }

    [Fact]
    public async Task Mismatched_amount_returns_failure()
    {
        var (db, ctx, _, _, order) = Seed(amount: 1980m);
        var c = NewController(db, ctx);

        var payload = new PaymentCallbackPayload(order.OrderNo, "WX-TX-3", 100m, true, "Wechat", "{}");
        var ack = await c.Simulate(payload, new FakeEnv("Development"), default);
        var ok = ack.Result as OkObjectResult;
        ((CallbackAck)ok!.Value!).Ok.Should().BeFalse();

        var saved = await db.PaymentOrders.FindAsync(order.Id);
        saved!.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task Failed_callback_marks_order_failed_but_does_not_renew()
    {
        var (db, ctx, tenant, _, order) = Seed();
        var c = NewController(db, ctx);

        var payload = new PaymentCallbackPayload(order.OrderNo, "WX-TX-4", order.Amount, false, "Wechat", "{}");
        var ack = await c.Simulate(payload, new FakeEnv("Development"), default);
        ((CallbackAck)((OkObjectResult)ack.Result!).Value!).Ok.Should().BeTrue();

        var saved = await db.PaymentOrders.FindAsync(order.Id);
        saved!.Status.Should().Be(PaymentStatus.Failed);

        (await db.Subscriptions.AnyAsync()).Should().BeFalse();
        (await db.Tenants.FindAsync(tenant.Id))!.Status.Should().Be(TenantStatus.Expired);
    }

    [Fact]
    public async Task Simulate_outside_development_returns_not_found()
    {
        var (db, ctx, _, _, order) = Seed();
        var c = NewController(db, ctx);
        var payload = new PaymentCallbackPayload(order.OrderNo, "WX-TX-5", order.Amount, true, "Wechat", "{}");

        var ack = await c.Simulate(payload, new FakeEnv("Production"), default);
        ack.Result.Should().BeOfType<NotFoundResult>();
    }

    private sealed class FakeEnv : IWebHostEnvironment
    {
        public FakeEnv(string envName) { EnvironmentName = envName; }
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "MassageSaas.Api";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = null!;
    }
}
