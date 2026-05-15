using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.DayCloses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace MassageSaas.UnitTests.Rooms;

/// <summary>验证计时房已结算收入并入日结预览的营业额与各支付方式桶。</summary>
public class TimedRoomRevenueTests
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
        db.Rooms.Add(new Room { Id = 1, TenantId = 1, StoreId = 1, RoomNo = "T1", IsTimedRoom = true, HourlyRate = 60m, IsActive = true });
        db.SaveChanges();
        return (db, ctx);
    }

    private static void AddSettledSession(ApplicationDbContext db, DateTime endedAt, decimal amount, PayMethod pay)
    {
        db.TimedRoomSessions.Add(new TimedRoomSession
        {
            TenantId = 1, StoreId = 1, RoomId = 1,
            StartedAt = endedAt.AddHours(-1), EndedAt = endedAt,
            HourlyRateSnapshot = 60m, BilledMinutes = 60,
            Amount = amount, PayMethod = pay,
            Status = TimedRoomSessionStatus.Settled
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task DayClosePreview_IncludesSettledTimedRoomRevenue()
    {
        var (db, ctx) = NewDb();
        var day = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        AddSettledSession(db, day.AddHours(10), 60m, PayMethod.Cash);
        AddSettledSession(db, day.AddHours(14), 90m, PayMethod.Wechat);
        // 不同日，不应计入
        AddSettledSession(db, day.AddDays(1).AddHours(2), 999m, PayMethod.Cash);

        var ctl = new DayClosesController(db, ctx, NullLogger<DayClosesController>.Instance);
        var resp = (await ctl.Preview(1, day, default)).Result as OkObjectResult;
        var p = resp!.Value as DayClosePreviewDto;

        p!.RevenueTotal.Should().Be(150m, "60 现金 + 90 微信");
        p.CashAmount.Should().Be(60m);
        p.WechatAmount.Should().Be(90m);
        p.ExpectedCash.Should().Be(60m, "计时房现金计入应收现金");
    }

    [Fact]
    public async Task DayClosePreview_IgnoresOpenAndCancelledSessions()
    {
        var (db, ctx) = NewDb();
        var day = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        db.TimedRoomSessions.Add(new TimedRoomSession
        {
            TenantId = 1, StoreId = 1, RoomId = 1,
            StartedAt = day.AddHours(9), HourlyRateSnapshot = 60m,
            Status = TimedRoomSessionStatus.Open
        });
        db.TimedRoomSessions.Add(new TimedRoomSession
        {
            TenantId = 1, StoreId = 1, RoomId = 1,
            StartedAt = day.AddHours(9), EndedAt = day.AddHours(10),
            HourlyRateSnapshot = 60m, Amount = 50m, PayMethod = PayMethod.Cash,
            Status = TimedRoomSessionStatus.Cancelled
        });
        await db.SaveChangesAsync();

        var ctl = new DayClosesController(db, ctx, NullLogger<DayClosesController>.Instance);
        var resp = (await ctl.Preview(1, day, default)).Result as OkObjectResult;
        var p = resp!.Value as DayClosePreviewDto;

        p!.RevenueTotal.Should().Be(0m, "计时中与已作废的不计入");
    }
}
