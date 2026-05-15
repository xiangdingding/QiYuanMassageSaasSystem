using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Rooms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace MassageSaas.UnitTests.Rooms;

public class TimedRoomTests
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

    private static Room AddRoom(ApplicationDbContext db, long id, bool timed, decimal rate)
    {
        var r = new Room
        {
            Id = id, TenantId = 1, StoreId = 1,
            RoomNo = $"R{id}", Capacity = 1, IsActive = true,
            IsTimedRoom = timed, HourlyRate = rate
        };
        db.Rooms.Add(r);
        db.SaveChanges();
        return r;
    }

    private static TimedRoomsController NewController(ApplicationDbContext db, TenantContext ctx) =>
        new(db, ctx, NullLogger<TimedRoomsController>.Instance);

    [Fact]
    public async Task Start_OpensSessionForTimedRoom()
    {
        var (db, ctx) = NewDb();
        AddRoom(db, 1, timed: true, rate: 30m);
        var ctl = NewController(db, ctx);

        var resp = (await ctl.Start(1, new StartTimedRoomRequest(null, "散客张", null), default)).Result as OkObjectResult;
        var dto = resp!.Value as TimedRoomSessionDto;

        dto!.Status.Should().Be("Open");
        dto.HourlyRateSnapshot.Should().Be(30m);
        dto.CustomerName.Should().Be("散客张");
    }

    [Fact]
    public async Task Start_RejectsNonTimedRoom()
    {
        var (db, ctx) = NewDb();
        AddRoom(db, 1, timed: false, rate: 0m);
        var ctl = NewController(db, ctx);
        var resp = (await ctl.Start(1, new StartTimedRoomRequest(null, null, null), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Start_RejectsWhenRoomAlreadyHasOpenSession()
    {
        var (db, ctx) = NewDb();
        AddRoom(db, 1, timed: true, rate: 30m);
        var ctl = NewController(db, ctx);
        await ctl.Start(1, new StartTimedRoomRequest(null, null, null), default);

        var second = (await ctl.Start(1, new StartTimedRoomRequest(null, null, null), default)).Result as ObjectResult;
        second!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Stop_BillsByElapsedTime()
    {
        var (db, ctx) = NewDb();
        AddRoom(db, 1, timed: true, rate: 60m);   // 60 元/小时 = 1 元/分钟
        // 直接造一条 90 分钟前开始的会话
        db.TimedRoomSessions.Add(new TimedRoomSession
        {
            Id = 500, TenantId = 1, StoreId = 1, RoomId = 1,
            StartedAt = DateTime.UtcNow.AddMinutes(-90),
            HourlyRateSnapshot = 60m,
            Status = TimedRoomSessionStatus.Open
        });
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var resp = (await ctl.Stop(500, new StopTimedRoomRequest("Cash"), default)).Result as OkObjectResult;
        var dto = resp!.Value as TimedRoomSessionDto;

        dto!.Status.Should().Be("Settled");
        dto.BilledMinutes.Should().BeInRange(90, 91);
        dto.Amount.Should().BeInRange(90m, 91m, "90 分钟 × 1 元/分钟");
        dto.PayMethod.Should().Be("Cash");
    }

    [Fact]
    public async Task Stop_RejectsUnpaidPayMethod()
    {
        var (db, ctx) = NewDb();
        AddRoom(db, 1, timed: true, rate: 30m);
        var ctl = NewController(db, ctx);
        var started = ((await ctl.Start(1, new StartTimedRoomRequest(null, null, null), default)).Result as OkObjectResult)!
            .Value as TimedRoomSessionDto;

        var resp = (await ctl.Stop(started!.Id, new StopTimedRoomRequest("Unpaid"), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Stop_RejectsAlreadySettledSession()
    {
        var (db, ctx) = NewDb();
        AddRoom(db, 1, timed: true, rate: 30m);
        var ctl = NewController(db, ctx);
        var started = ((await ctl.Start(1, new StartTimedRoomRequest(null, null, null), default)).Result as OkObjectResult)!
            .Value as TimedRoomSessionDto;
        await ctl.Stop(started!.Id, new StopTimedRoomRequest("Cash"), default);

        var second = (await ctl.Stop(started.Id, new StopTimedRoomRequest("Cash"), default)).Result as ObjectResult;
        second!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Stop_MemberCard_DeductsMemberBalance()
    {
        var (db, ctx) = NewDb();
        AddRoom(db, 1, timed: true, rate: 60m);
        db.Members.Add(new Member
        {
            Id = 500, TenantId = 1, StoreId = 1, CardNo = "C500", Phone = "138",
            Balance = 200m, Discount = 1m
        });
        db.TimedRoomSessions.Add(new TimedRoomSession
        {
            Id = 600, TenantId = 1, StoreId = 1, RoomId = 1, MemberId = 500,
            StartedAt = DateTime.UtcNow.AddMinutes(-60),
            HourlyRateSnapshot = 60m, Status = TimedRoomSessionStatus.Open
        });
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var resp = (await ctl.Stop(600, new StopTimedRoomRequest("MemberCard"), default)).Result as OkObjectResult;
        var dto = resp!.Value as TimedRoomSessionDto;

        var member = await db.Members.FirstAsync(m => m.Id == 500);
        member.Balance.Should().Be(200m - dto!.Amount);
        member.TotalConsumed.Should().Be(dto.Amount);
    }

    [Fact]
    public async Task Stop_MemberCard_RejectsWhenBalanceInsufficient()
    {
        var (db, ctx) = NewDb();
        AddRoom(db, 1, timed: true, rate: 60m);
        db.Members.Add(new Member
        {
            Id = 500, TenantId = 1, StoreId = 1, CardNo = "C500", Phone = "138",
            Balance = 10m, Discount = 1m
        });
        db.TimedRoomSessions.Add(new TimedRoomSession
        {
            Id = 600, TenantId = 1, StoreId = 1, RoomId = 1, MemberId = 500,
            StartedAt = DateTime.UtcNow.AddMinutes(-60),
            HourlyRateSnapshot = 60m, Status = TimedRoomSessionStatus.Open
        });
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var resp = (await ctl.Stop(600, new StopTimedRoomRequest("MemberCard"), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(400);

        var session = await db.TimedRoomSessions.FirstAsync(s => s.Id == 600);
        session.Status.Should().Be(TimedRoomSessionStatus.Open, "余额不足结算失败，计时记录应保持开放");
    }

    [Fact]
    public async Task Stop_MemberCard_RejectsWalkInSession()
    {
        var (db, ctx) = NewDb();
        AddRoom(db, 1, timed: true, rate: 60m);
        var ctl = NewController(db, ctx);
        var started = ((await ctl.Start(1, new StartTimedRoomRequest(null, "散客", null), default)).Result as OkObjectResult)!
            .Value as TimedRoomSessionDto;

        var resp = (await ctl.Stop(started!.Id, new StopTimedRoomRequest("MemberCard"), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Cancel_VoidsOpenSession_AndFreesRoom()
    {
        var (db, ctx) = NewDb();
        AddRoom(db, 1, timed: true, rate: 30m);
        var ctl = NewController(db, ctx);
        var started = ((await ctl.Start(1, new StartTimedRoomRequest(null, null, null), default)).Result as OkObjectResult)!
            .Value as TimedRoomSessionDto;

        var cancelled = ((await ctl.Cancel(started!.Id, default)).Result as OkObjectResult)!.Value as TimedRoomSessionDto;
        cancelled!.Status.Should().Be("Cancelled");

        // 作废后可重新开台
        var restart = (await ctl.Start(1, new StartTimedRoomRequest(null, null, null), default)).Result as OkObjectResult;
        restart!.Value.Should().BeOfType<TimedRoomSessionDto>();
    }
}
