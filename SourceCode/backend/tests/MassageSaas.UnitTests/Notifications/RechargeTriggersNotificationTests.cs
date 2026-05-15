using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace MassageSaas.UnitTests.Notifications;

public class RechargeTriggersNotificationTests
{
    [Fact]
    public async Task Recharge_EnqueuesArrivalNotification()
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
        db.Members.Add(new Member
        {
            Id = 100, TenantId = 1, StoreId = 1,
            CardNo = "C100", Phone = "13800", Name = "张三",
            Discount = 1m
        });
        await db.SaveChangesAsync();

        var ctl = new MembersController(db, ctx, NullLogger<MembersController>.Instance);
        await ctl.Recharge(new RechargeRequest(100, 500m, 50m, "Cash", "周末活动"), default);

        var n = await db.NotificationOutbox.FirstAsync();
        n.Kind.Should().Be(NotificationKind.RechargeArrived);
        n.MemberId.Should().Be(100);
        n.RecipientPhone.Should().Be("13800");
        n.Body.Should().Contain("500");
        n.Body.Should().Contain("赠送");
        n.Status.Should().Be(NotificationStatus.Pending);
    }
}
