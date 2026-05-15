using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Complaints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace MassageSaas.UnitTests.Complaints;

public class ComplaintsTests
{
    private static (ApplicationDbContext Db, TenantContext Ctx) NewDb()
    {
        var ctx = new TenantContext { TenantId = 1, UserId = 99 };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"db_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();
        db.Tenants.Add(new Tenant { Id = 1, Name = "T", ContactPhone = "x" });
        db.Stores.Add(new Store { Id = 1, TenantId = 1, Name = "总店", IsActive = true });
        db.SaveChanges();
        return (db, ctx);
    }

    private static User AddTech(ApplicationDbContext db, long id, string name)
    {
        var u = new User
        {
            Id = id, TenantId = 1, StoreId = 1,
            Username = $"t{id}", PasswordHash = "x",
            Role = UserRole.Technician, IsActive = true,
            EmployeeNo = (int)id, RealName = name
        };
        db.Users.Add(u);
        return u;
    }

    private static (Order Order, OrderItem Item) SeedOrder(ApplicationDbContext db, long techId, OrderStatus status = OrderStatus.InProgress)
    {
        var orderId = Random.Shared.NextInt64(1_000_000, 9_000_000);
        var order = new Order
        {
            Id = orderId, TenantId = 1, StoreId = 1,
            OrderNo = $"O{orderId}",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
        db.Orders.Add(order);
        var item = new OrderItem
        {
            TenantId = 1, OrderId = orderId, Order = order,
            ServiceId = 1, ServiceName = "60min肩颈",
            TechnicianId = techId,
            DurationMinutes = 60, UnitPrice = 100m, Quantity = 1, ItemTotal = 100m
        };
        db.OrderItems.Add(item);
        db.SaveChanges();
        return (order, item);
    }

    private static ComplaintsController NewController(ApplicationDbContext db, TenantContext ctx) =>
        new(db, ctx, NullLogger<ComplaintsController>.Instance);

    [Fact]
    public async Task Create_RecordsComplaintAndEnqueuesNotification()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, "王师傅");
        var (_, item) = SeedOrder(db, 10);

        var ctl = NewController(db, ctx);
        var resp = (await ctl.Create(new CreateComplaintRequest(item.Id, "态度差,力度不合适", "客户感觉力度太大"), default))
            .Result as OkObjectResult;
        var dto = resp!.Value as ComplaintDto;

        dto!.OriginalTechnicianName.Should().Be("王师傅");
        dto.Status.Should().Be("Pending");
        dto.Tags.Should().Be("态度差,力度不合适");

        var n = await db.NotificationOutbox.SingleAsync();
        n.Kind.Should().Be(NotificationKind.ServiceComplaintAlert);
        n.Body.Should().Contain("王师傅");
        n.Body.Should().Contain("态度差");
    }

    [Fact]
    public async Task Create_RejectsDuplicatePendingComplaintForSameItem()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, "王师傅");
        var (_, item) = SeedOrder(db, 10);
        var ctl = NewController(db, ctx);
        await ctl.Create(new CreateComplaintRequest(item.Id, null, null), default);

        var second = (await ctl.Create(new CreateComplaintRequest(item.Id, null, null), default))
            .Result as ObjectResult;
        second!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Resolve_Reassigned_TransfersTechnicianAndFlagsItem()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, "王师傅");
        AddTech(db, 11, "李师傅");
        var (_, item) = SeedOrder(db, 10);
        var ctl = NewController(db, ctx);
        var created = ((await ctl.Create(new CreateComplaintRequest(item.Id, "态度差", null), default))
            .Result as OkObjectResult)!.Value as ComplaintDto;

        var resolved = ((await ctl.Resolve(created!.Id,
            new ResolveComplaintRequest("Reassigned", 11, "改派李师傅"), default))
            .Result as OkObjectResult)!.Value as ComplaintDto;

        resolved!.Status.Should().Be("Resolved");
        resolved.Resolution.Should().Be("Reassigned");
        resolved.ReassignedToTechnicianId.Should().Be(11);

        var freshItem = await db.OrderItems.AsNoTracking().FirstAsync(i => i.Id == item.Id);
        freshItem.TechnicianId.Should().Be(11);
        freshItem.PreviousTechnicianId.Should().Be(10);
        freshItem.ComplaintTransferred.Should().BeTrue();
        freshItem.TransferReason.Should().Contain("投诉改派");
    }

    [Fact]
    public async Task Resolve_Reassigned_RejectsSameTechnician()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, "王师傅");
        var (_, item) = SeedOrder(db, 10);
        var ctl = NewController(db, ctx);
        var created = ((await ctl.Create(new CreateComplaintRequest(item.Id, null, null), default))
            .Result as OkObjectResult)!.Value as ComplaintDto;
        var resp = (await ctl.Resolve(created!.Id,
            new ResolveComplaintRequest("Reassigned", 10, null), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Resolve_Reassigned_RejectsWhenOrderCompleted()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, "王师傅");
        AddTech(db, 11, "李师傅");
        var (order, item) = SeedOrder(db, 10);
        var ctl = NewController(db, ctx);
        var created = ((await ctl.Create(new CreateComplaintRequest(item.Id, null, null), default))
            .Result as OkObjectResult)!.Value as ComplaintDto;

        order.Status = OrderStatus.Completed;
        await db.SaveChangesAsync();

        var resp = (await ctl.Resolve(created!.Id,
            new ResolveComplaintRequest("Reassigned", 11, null), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Resolve_Apologized_DoesNotTouchOrderItem()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, "王师傅");
        var (_, item) = SeedOrder(db, 10);
        var ctl = NewController(db, ctx);
        var created = ((await ctl.Create(new CreateComplaintRequest(item.Id, null, null), default))
            .Result as OkObjectResult)!.Value as ComplaintDto;

        var resolved = ((await ctl.Resolve(created!.Id,
            new ResolveComplaintRequest("Apologized", null, "送了一张体验券"), default))
            .Result as OkObjectResult)!.Value as ComplaintDto;
        resolved!.Resolution.Should().Be("Apologized");

        var freshItem = await db.OrderItems.AsNoTracking().FirstAsync(i => i.Id == item.Id);
        freshItem.TechnicianId.Should().Be(10);
        freshItem.ComplaintTransferred.Should().BeFalse();
    }

    [Fact]
    public async Task Resolve_AlreadyResolvedComplaint_Rejects()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, "王师傅");
        var (_, item) = SeedOrder(db, 10);
        var ctl = NewController(db, ctx);
        var created = ((await ctl.Create(new CreateComplaintRequest(item.Id, null, null), default))
            .Result as OkObjectResult)!.Value as ComplaintDto;
        await ctl.Resolve(created!.Id, new ResolveComplaintRequest("NoAction", null, null), default);

        var second = (await ctl.Resolve(created.Id,
            new ResolveComplaintRequest("NoAction", null, null), default)).Result as ObjectResult;
        second!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task List_FiltersByTechnicianAndStatus()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, "王师傅");
        AddTech(db, 11, "李师傅");
        var (_, item10) = SeedOrder(db, 10);
        var (_, item11) = SeedOrder(db, 11);
        var ctl = NewController(db, ctx);
        var c1 = ((await ctl.Create(new CreateComplaintRequest(item10.Id, "脏", null), default))
            .Result as OkObjectResult)!.Value as ComplaintDto;
        await ctl.Create(new CreateComplaintRequest(item11.Id, "态度差", null), default);
        await ctl.Resolve(c1!.Id, new ResolveComplaintRequest("NoAction", null, null), default);

        var pendingTech10 = ((await ctl.List(null, 10, "Pending", null, null, 1, 20, default)).Result as OkObjectResult)!.Value
            as MassageSaas.Shared.Common.PagedResult<ComplaintDto>;
        pendingTech10!.Total.Should().Be(0);

        var resolved = ((await ctl.List(null, null, "Resolved", null, null, 1, 20, default)).Result as OkObjectResult)!.Value
            as MassageSaas.Shared.Common.PagedResult<ComplaintDto>;
        resolved!.Total.Should().Be(1);
        resolved.Items.Single().OriginalTechnicianId.Should().Be(10);
    }
}
