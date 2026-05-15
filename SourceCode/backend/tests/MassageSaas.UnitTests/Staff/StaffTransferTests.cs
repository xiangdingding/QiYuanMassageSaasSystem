using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Staff;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MassageSaas.UnitTests.Staff;

public class StaffTransferTests
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
        db.Stores.Add(new Store { Id = 1, TenantId = 1, Name = "总店", IsActive = true });
        db.Stores.Add(new Store { Id = 2, TenantId = 1, Name = "分店", IsActive = true });
        db.SaveChanges();
        return (db, ctx);
    }

    private static User AddTech(ApplicationDbContext db, long id, long storeId)
    {
        var u = new User
        {
            Id = id, TenantId = 1, StoreId = storeId,
            Username = $"t{id}", PasswordHash = "x",
            Role = UserRole.Technician, IsActive = true, EmployeeNo = (int)id
        };
        db.Users.Add(u);
        db.SaveChanges();
        return u;
    }

    private static StaffController NewController(ApplicationDbContext db, TenantContext ctx) => new(db, ctx);

    [Fact]
    public async Task Transfer_Permanent_MovesUserToTargetStore()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, storeId: 1);
        var ctl = NewController(db, ctx);

        var resp = (await ctl.Transfer(10,
            new TransferStaffRequest(2, "Permanent", null, "分店缺人"), default)).Result as OkObjectResult;
        var dto = resp!.Value as StaffTransferDto;

        dto!.FromStoreId.Should().Be(1);
        dto.ToStoreId.Should().Be(2);
        dto.Kind.Should().Be("Permanent");
        dto.Status.Should().Be("InEffect");

        var user = await db.Users.FirstAsync(u => u.Id == 10);
        user.StoreId.Should().Be(2);
    }

    [Fact]
    public async Task Transfer_ResetsTechnicianQueueToNewStore()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, storeId: 1);
        db.TechnicianQueues.Add(new TechnicianQueue
        {
            TenantId = 1, StoreId = 1, TechnicianId = 10,
            State = QueueState.OnDuty, QueuePosition = 3, TodayRoundCount = 5
        });
        await db.SaveChangesAsync();
        var ctl = NewController(db, ctx);

        await ctl.Transfer(10, new TransferStaffRequest(2, "Permanent", null, null), default);

        var q = await db.TechnicianQueues.FirstAsync(x => x.TechnicianId == 10);
        q.StoreId.Should().Be(2);
        q.State.Should().Be(QueueState.OffDuty);
        q.QueuePosition.Should().Be(0);
        q.TodayRoundCount.Should().Be(0);
    }

    [Fact]
    public async Task Transfer_RejectsSameStore()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, storeId: 1);
        var ctl = NewController(db, ctx);
        var resp = (await ctl.Transfer(10,
            new TransferStaffRequest(1, "Permanent", null, null), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Transfer_Temporary_RequiresReturnDate()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, storeId: 1);
        var ctl = NewController(db, ctx);
        var resp = (await ctl.Transfer(10,
            new TransferStaffRequest(2, "Temporary", null, null), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Return_Temporary_MovesUserBack()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, storeId: 1);
        var ctl = NewController(db, ctx);
        var created = ((await ctl.Transfer(10,
            new TransferStaffRequest(2, "Temporary", DateTime.UtcNow.AddDays(30), "旺季支援"), default))
            .Result as OkObjectResult)!.Value as StaffTransferDto;

        (await db.Users.FirstAsync(u => u.Id == 10)).StoreId.Should().Be(2);

        var returned = ((await ctl.ReturnTransfer(created!.Id, default)).Result as OkObjectResult)!.Value as StaffTransferDto;
        returned!.Status.Should().Be("Returned");
        returned.ReturnedAt.Should().NotBeNull();

        (await db.Users.FirstAsync(u => u.Id == 10)).StoreId.Should().Be(1);
    }

    [Fact]
    public async Task Return_RejectsPermanentTransfer()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, storeId: 1);
        var ctl = NewController(db, ctx);
        var created = ((await ctl.Transfer(10,
            new TransferStaffRequest(2, "Permanent", null, null), default))
            .Result as OkObjectResult)!.Value as StaffTransferDto;

        var resp = (await ctl.ReturnTransfer(created!.Id, default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Transfer_RejectsDuplicateEmployeeNoAtTarget()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, storeId: 1);   // EmployeeNo = 10
        AddTech(db, 20, storeId: 2);   // EmployeeNo = 20
        var conflicting = await db.Users.FirstAsync(u => u.Id == 20);
        conflicting.EmployeeNo = 10;   // 与待调动员工同号
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var resp = (await ctl.Transfer(10,
            new TransferStaffRequest(2, "Permanent", null, null), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task ListTransfers_FiltersByUser()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, storeId: 1);
        AddTech(db, 11, storeId: 1);
        var ctl = NewController(db, ctx);
        await ctl.Transfer(10, new TransferStaffRequest(2, "Permanent", null, null), default);
        await ctl.Transfer(11, new TransferStaffRequest(2, "Permanent", null, null), default);

        var rows = ((await ctl.ListTransfers(10, null, null, default)).Result as OkObjectResult)!
            .Value as IReadOnlyList<StaffTransferDto>;
        rows!.Should().HaveCount(1);
        rows![0].UserId.Should().Be(10);
    }
}
