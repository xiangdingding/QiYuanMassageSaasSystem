using System.Security.Claims;
using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Appointments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace MassageSaas.UnitTests.Appointments;

public class AppointmentControllerTests
{
    private static (ApplicationDbContext Db, TenantContext Ctx, Store Store) Seed()
    {
        var ctx = new TenantContext();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"appt_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();
        var tenant = new Tenant { Id = 1, Name = "测试按摩店", ContactPhone = "13800000000", Status = TenantStatus.Active };
        var store = new Store { Id = 1, TenantId = 1, Name = "总店", IsActive = true };
        db.Tenants.Add(tenant);
        db.Stores.Add(store);
        db.SaveChanges();
        return (db, ctx, store);
    }

    private static AppointmentsController NewController(ApplicationDbContext db, TenantContext ctx, ClaimsPrincipal? user = null)
    {
        var c = new AppointmentsController(db, ctx, NullLogger<AppointmentsController>.Instance);
        c.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user ?? new ClaimsPrincipal(new ClaimsIdentity()) }
        };
        return c;
    }

    [Fact]
    public async Task Create_succeeds_for_valid_anonymous_request()
    {
        var (db, ctx, store) = Seed();
        var c = NewController(db, ctx);

        var req = new CreateAppointmentRequest(
            store.Id, null, null,
            "张三", "13800001111", "openid-1",
            DateTime.UtcNow.AddHours(2), 2, "靠窗");

        var result = await c.Create(req, default);
        var created = result.Result as CreatedAtActionResult;
        created.Should().NotBeNull();
        var dto = created!.Value as AppointmentDto;
        dto!.Status.Should().Be(nameof(AppointmentStatus.Pending));
        dto.PartySize.Should().Be(2);
        dto.StoreName.Should().Be("总店");

        var saved = await db.Appointments.FirstAsync();
        saved.TenantId.Should().Be(1);
        saved.CustomerOpenId.Should().Be("openid-1");
    }

    [Fact]
    public async Task Create_rejects_past_arrival_time()
    {
        var (db, ctx, store) = Seed();
        var c = NewController(db, ctx);

        var req = new CreateAppointmentRequest(
            store.Id, null, null,
            "张三", "13800001111", null,
            DateTime.UtcNow.AddHours(-1), 1, null);

        var result = await c.Create(req, default);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Cancel_anonymous_with_matching_phone_succeeds()
    {
        var (db, ctx, store) = Seed();
        var a = new Appointment
        {
            TenantId = 1, StoreId = store.Id,
            CustomerName = "李四", CustomerPhone = "13900009999",
            ExpectedArriveAt = DateTime.UtcNow.AddHours(3),
            PartySize = 1, Status = AppointmentStatus.Pending
        };
        db.Appointments.Add(a);
        await db.SaveChangesAsync();

        var c = NewController(db, ctx);
        var result = await c.Cancel(a.Id, new CancelAppointmentRequest("不来了"), null, "13900009999", default);
        result.Result.Should().BeOfType<OkObjectResult>();

        var reloaded = await db.Appointments.FindAsync(a.Id);
        reloaded!.Status.Should().Be(AppointmentStatus.Cancelled);
        reloaded.CancelReason.Should().Be("不来了");
    }

    [Fact]
    public async Task Cancel_anonymous_with_wrong_phone_is_forbidden()
    {
        var (db, ctx, store) = Seed();
        var a = new Appointment
        {
            TenantId = 1, StoreId = store.Id,
            CustomerName = "李四", CustomerPhone = "13900009999",
            ExpectedArriveAt = DateTime.UtcNow.AddHours(3),
            PartySize = 1, Status = AppointmentStatus.Pending
        };
        db.Appointments.Add(a);
        await db.SaveChangesAsync();

        var c = NewController(db, ctx);
        var result = await c.Cancel(a.Id, new CancelAppointmentRequest("hack"), null, "13800000000", default);
        result.Result.Should().BeOfType<ForbidResult>();
    }
}
