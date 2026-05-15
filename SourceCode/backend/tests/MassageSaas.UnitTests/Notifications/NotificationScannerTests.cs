using FluentAssertions;
using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Notifications;
using MassageSaas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace MassageSaas.UnitTests.Notifications;

public class NotificationScannerTests
{
    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow { get; set; }
    }

    private static (ApplicationDbContext Db, TenantContext Ctx, FixedClock Clock, NotificationScanner Scanner)
        NewScanner(DateTime now)
    {
        var ctx = new TenantContext { TenantId = 1, UserId = 1 };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"db_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();

        db.Tenants.Add(new Tenant { Id = 1, Name = "T", ContactPhone = "x" });
        db.Stores.Add(new Store { Id = 1, TenantId = 1, Name = "总店", IsActive = true });
        db.SaveChanges();

        var clock = new FixedClock { UtcNow = now };
        var scanner = new NotificationScanner(db, ctx, clock, NullLogger<NotificationScanner>.Instance);
        return (db, ctx, clock, scanner);
    }

    [Fact]
    public async Task Birthday_EnqueuedForTodaysBirthday()
    {
        var today = new DateTime(2026, 5, 15, 1, 0, 0, DateTimeKind.Utc);
        var (db, _, _, scanner) = NewScanner(today);
        db.Members.Add(new Member
        {
            Id = 100, TenantId = 1, StoreId = 1,
            CardNo = "C100", Phone = "13800",
            Name = "张三",
            Birthday = new DateTime(1980, 5, 15)
        });
        db.Members.Add(new Member
        {
            Id = 101, TenantId = 1, StoreId = 1,
            CardNo = "C101", Phone = "13801",
            Birthday = new DateTime(1990, 7, 1) // 不是今天
        });
        await db.SaveChangesAsync();

        var added = await scanner.ScanAndEnqueueAsync(default);

        added.Should().Be(1);
        var n = await db.NotificationOutbox.FirstAsync();
        n.Kind.Should().Be(NotificationKind.MemberBirthday);
        n.MemberId.Should().Be(100);
        n.Body.Should().Contain("张三");
    }

    [Fact]
    public async Task Birthday_IsIdempotent_BySecondScan()
    {
        var today = new DateTime(2026, 5, 15, 1, 0, 0, DateTimeKind.Utc);
        var (db, _, _, scanner) = NewScanner(today);
        db.Members.Add(new Member
        {
            Id = 100, TenantId = 1, StoreId = 1,
            CardNo = "C100", Phone = "13800",
            Birthday = new DateTime(1980, 5, 15)
        });
        await db.SaveChangesAsync();

        await scanner.ScanAndEnqueueAsync(default);
        var second = await scanner.ScanAndEnqueueAsync(default);

        second.Should().Be(0);
        (await db.NotificationOutbox.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Birthday_SkipsClosedMembers()
    {
        var today = new DateTime(2026, 5, 15, 1, 0, 0, DateTimeKind.Utc);
        var (db, _, _, scanner) = NewScanner(today);
        db.Members.Add(new Member
        {
            Id = 100, TenantId = 1, StoreId = 1,
            CardNo = "C100", Phone = "13800",
            Birthday = new DateTime(1980, 5, 15),
            IsActive = false, ClosedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var added = await scanner.ScanAndEnqueueAsync(default);
        added.Should().Be(0);
    }

    [Fact]
    public async Task PackageExpiring_EnqueuedWithinSevenDays()
    {
        var now = new DateTime(2026, 5, 15, 1, 0, 0, DateTimeKind.Utc);
        var (db, _, _, scanner) = NewScanner(now);
        var m = new Member { Id = 100, TenantId = 1, StoreId = 1, CardNo = "C100", Phone = "13800", Name = "张三" };
        db.Members.Add(m);
        db.MemberPackages.Add(new MemberPackage
        {
            Id = 500, TenantId = 1, StoreId = 1, MemberId = 100, Member = m,
            Kind = MemberPackageKind.Period, Title = "季度卡",
            ExpiresAt = now.AddDays(3),
            Status = MemberPackageStatus.Active
        });
        db.MemberPackages.Add(new MemberPackage
        {
            Id = 501, TenantId = 1, StoreId = 1, MemberId = 100, Member = m,
            Kind = MemberPackageKind.Period, Title = "年卡",
            ExpiresAt = now.AddDays(20), // 7 天外
            Status = MemberPackageStatus.Active
        });
        db.MemberPackages.Add(new MemberPackage
        {
            Id = 502, TenantId = 1, StoreId = 1, MemberId = 100, Member = m,
            Kind = MemberPackageKind.Counter, Title = "计次卡",
            ExpiresAt = now.AddDays(2), // 不是 Period，不通知
            Status = MemberPackageStatus.Active
        });
        await db.SaveChangesAsync();

        var added = await scanner.ScanAndEnqueueAsync(default);

        added.Should().Be(1);
        var n = await db.NotificationOutbox.FirstAsync();
        n.Kind.Should().Be(NotificationKind.MemberPackageExpiring);
        n.RelatedEntityId.Should().Be(500);
        n.Body.Should().Contain("季度卡");
    }

    [Fact]
    public async Task AppointmentReminder_EnqueuedWithin30Min()
    {
        var now = new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc);
        var (db, _, _, scanner) = NewScanner(now);
        db.Appointments.Add(new Appointment
        {
            Id = 200, TenantId = 1, StoreId = 1,
            CustomerName = "李四", CustomerPhone = "13900",
            Status = AppointmentStatus.Confirmed,
            ExpectedArriveAt = now.AddMinutes(15),
            PartySize = 1
        });
        db.Appointments.Add(new Appointment
        {
            Id = 201, TenantId = 1, StoreId = 1,
            CustomerName = "王五", CustomerPhone = "13901",
            Status = AppointmentStatus.Confirmed,
            ExpectedArriveAt = now.AddMinutes(60), // 超过 30 分钟
            PartySize = 1
        });
        db.Appointments.Add(new Appointment
        {
            Id = 202, TenantId = 1, StoreId = 1,
            CustomerName = "赵六", CustomerPhone = "13902",
            Status = AppointmentStatus.Cancelled, // 已取消，跳过
            ExpectedArriveAt = now.AddMinutes(10),
            PartySize = 1
        });
        await db.SaveChangesAsync();

        var added = await scanner.ScanAndEnqueueAsync(default);
        added.Should().Be(1);
        var n = await db.NotificationOutbox.FirstAsync();
        n.Kind.Should().Be(NotificationKind.AppointmentReminder);
        n.RelatedEntityId.Should().Be(200);
    }
}
