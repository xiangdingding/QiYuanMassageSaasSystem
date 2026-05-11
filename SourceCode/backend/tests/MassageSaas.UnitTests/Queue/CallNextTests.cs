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

public class CallNextTests
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
        db.SaveChanges();
        return (db, ctx);
    }

    private static User AddTech(ApplicationDbContext db, long id, int? maxRounds, int empNo)
    {
        var u = new User
        {
            Id = id, TenantId = 1, StoreId = 1,
            Username = $"t{id}", PasswordHash = "x",
            Role = UserRole.Technician, IsActive = true,
            EmployeeNo = empNo, MaxRoundsPerDay = maxRounds ?? 0
        };
        db.Users.Add(u);
        return u;
    }

    private static TechnicianQueue OnDuty(ApplicationDbContext db, long techId, int rounds, int pos)
    {
        var q = new TechnicianQueue
        {
            TenantId = 1, StoreId = 1, TechnicianId = techId,
            State = QueueState.OnDuty, QueuePosition = pos,
            TodayRoundCount = rounds, EnteredAt = DateTime.UtcNow.AddHours(-1)
        };
        db.TechnicianQueues.Add(q);
        return q;
    }

    [Fact]
    public async Task CallNext_SkipsTechniciansAtOrOverDailyCap()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, maxRounds: 5, empNo: 1);   // 已达上限
        AddTech(db, 20, maxRounds: 5, empNo: 2);   // 未达
        OnDuty(db, 10, rounds: 5, pos: 1);          // 应跳过
        OnDuty(db, 20, rounds: 3, pos: 2);          // 应被叫
        await db.SaveChangesAsync();

        var ctl = new TechnicianQueueController(db, ctx);
        var result = (await ctl.CallNext(new CallNextRequest(StoreId: 1), default)).Result as OkObjectResult;
        var dto = result!.Value as CallNextResultDto;

        dto!.TechnicianId.Should().Be(20, "tech 10 已达 MaxRoundsPerDay=5");
    }

    [Fact]
    public async Task CallNext_TreatsZeroMaxAsUnlimited()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, maxRounds: 0, empNo: 1);  // 不限
        OnDuty(db, 10, rounds: 99, pos: 1);
        await db.SaveChangesAsync();

        var ctl = new TechnicianQueueController(db, ctx);
        var result = (await ctl.CallNext(new CallNextRequest(StoreId: 1), default)).Result as OkObjectResult;
        var dto = result!.Value as CallNextResultDto;

        dto!.TechnicianId.Should().Be(10);
    }

    [Fact]
    public async Task CallNext_ReturnsNullWhenAllTechniciansOverloaded()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10, maxRounds: 5, empNo: 1);
        AddTech(db, 20, maxRounds: 3, empNo: 2);
        OnDuty(db, 10, rounds: 5, pos: 1);
        OnDuty(db, 20, rounds: 4, pos: 2);
        await db.SaveChangesAsync();

        var ctl = new TechnicianQueueController(db, ctx);
        var result = (await ctl.CallNext(new CallNextRequest(StoreId: 1), default)).Result as OkObjectResult;
        var dto = result!.Value as CallNextResultDto;

        dto!.TechnicianId.Should().BeNull();
    }
}
