using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Queue;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MassageSaas.UnitTests.Queue;

public class MyQueueBriefingTests
{
    private const long TechId = 10;

    private static (ApplicationDbContext Db, TenantContext Ctx) NewDb()
    {
        var ctx = new TenantContext { TenantId = 1, UserId = TechId };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"db_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();

        db.Stores.Add(new Store { Id = 1, TenantId = 1, Name = "总店", IsActive = true });
        db.Users.Add(new User
        {
            Id = TechId, TenantId = 1, StoreId = 1,
            Username = "t10", PasswordHash = "x",
            Role = UserRole.Technician, IsActive = true, EmployeeNo = 1
        });
        db.SaveChanges();
        return (db, ctx);
    }

    private static long SeedOrder(
        ApplicationDbContext db,
        OrderStatus status,
        long? memberId,
        string roomNo = "201")
    {
        var orderId = Random.Shared.NextInt64(1_000_000, 9_000_000);
        db.Orders.Add(new Order
        {
            Id = orderId, TenantId = 1, StoreId = 1,
            OrderNo = $"O{orderId}",
            Status = status,
            MemberId = memberId,
            CreatedAt = DateTime.UtcNow
        });
        db.OrderItems.Add(new OrderItem
        {
            TenantId = 1, OrderId = orderId,
            ServiceId = 1, ServiceName = "60min肩颈",
            TechnicianId = TechId,
            DurationMinutes = 60, UnitPrice = 100m, Quantity = 1, ItemTotal = 100m,
            RoomNoSnapshot = roomNo
        });
        db.SaveChanges();
        return orderId;
    }

    [Fact]
    public async Task Me_ReturnsCustomerBriefing_WhenMemberHasNotes()
    {
        var (db, ctx) = NewDb();
        db.Members.Add(new Member
        {
            Id = 500, TenantId = 1, StoreId = 1,
            CardNo = "C500", Phone = "13800",
            Name = "李先生", Gender = "男",
            PreferenceNotes = "喜欢重力，避开颈椎",
            HealthNotes = "高血压，禁用强力压颈"
        });
        await db.SaveChangesAsync();
        SeedOrder(db, OrderStatus.InProgress, 500);

        var ctl = new TechnicianQueueController(db, ctx);
        var resp = (await ctl.Me(default)).Result as OkObjectResult;
        var dto = resp!.Value as MyQueueDto;

        dto!.CurrentRoomNo.Should().Be("201");
        dto.CurrentCustomerName.Should().Be("李先生");
        dto.CurrentCustomerGender.Should().Be("男");
        dto.CurrentCustomerPreferences.Should().Contain("重力");
        dto.CurrentCustomerHealth.Should().Contain("高血压");
        dto.CurrentCustomerHasNotes.Should().BeTrue();
    }

    [Fact]
    public async Task Me_HasNotesIsFalse_WhenMemberHasNoNotes()
    {
        var (db, ctx) = NewDb();
        db.Members.Add(new Member
        {
            Id = 500, TenantId = 1, StoreId = 1,
            CardNo = "C500", Phone = "13800", Name = "Plain"
        });
        await db.SaveChangesAsync();
        SeedOrder(db, OrderStatus.InProgress, 500);

        var ctl = new TechnicianQueueController(db, ctx);
        var dto = ((await ctl.Me(default)).Result as OkObjectResult)!.Value as MyQueueDto;

        dto!.CurrentCustomerName.Should().Be("Plain");
        dto.CurrentCustomerHasNotes.Should().BeFalse();
        dto.CurrentCustomerPreferences.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Me_AllCustomerFieldsNull_ForWalkInOrder()
    {
        var (db, ctx) = NewDb();
        SeedOrder(db, OrderStatus.InProgress, memberId: null);

        var ctl = new TechnicianQueueController(db, ctx);
        var dto = ((await ctl.Me(default)).Result as OkObjectResult)!.Value as MyQueueDto;

        dto!.CurrentOrderId.Should().NotBeNull();
        dto.CurrentCustomerName.Should().BeNull();
        dto.CurrentCustomerHasNotes.Should().BeFalse();
    }

    [Fact]
    public async Task Me_NoCurrentOrder_WhenOnlyCompletedExists()
    {
        var (db, ctx) = NewDb();
        SeedOrder(db, OrderStatus.Completed, memberId: null);

        var ctl = new TechnicianQueueController(db, ctx);
        var dto = ((await ctl.Me(default)).Result as OkObjectResult)!.Value as MyQueueDto;

        dto!.CurrentOrderId.Should().BeNull();
        dto.CurrentRoomNo.Should().BeNull();
        dto.CurrentCustomerHasNotes.Should().BeFalse();
    }
}
