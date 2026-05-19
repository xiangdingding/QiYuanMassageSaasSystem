using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MassageSaas.UnitTests.Reports;

public class ReportsAnalyticsTests
{
    private static ApplicationDbContext NewDb(out ReportsController controller)
    {
        var ctx = new TenantContext();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"reports_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();
        db.Tenants.Add(new Tenant { Id = 1, Name = "测试店", ContactPhone = "13800000000", Status = TenantStatus.Active });
        db.Stores.Add(new Store { Id = 1, TenantId = 1, Name = "总店", IsActive = true });
        controller = new ReportsController(db, ctx);
        return db;
    }

    private static Order CompletedOrder(long id, long? memberId, DateTime completedAt) => new()
    {
        Id = id, TenantId = 1, StoreId = 1, OrderNo = $"O{id}",
        MemberId = memberId, Status = OrderStatus.Completed, CompletedAt = completedAt
    };

    [Fact]
    public async Task MemberAnalysis_buckets_members_by_recency()
    {
        var db = NewDb(out var c);
        var now = DateTime.UtcNow;
        var old = now.AddMonths(-6);

        // 4 个会员：A 活跃且复购、B 沉睡、C 流失、D 从未消费且本月新增
        var mA = new Member { Id = 1, TenantId = 1, StoreId = 1, CardNo = "A", Phone = "1" };
        var mB = new Member { Id = 2, TenantId = 1, StoreId = 1, CardNo = "B", Phone = "2" };
        var mC = new Member { Id = 3, TenantId = 1, StoreId = 1, CardNo = "C", Phone = "3" };
        var mD = new Member { Id = 4, TenantId = 1, StoreId = 1, CardNo = "D", Phone = "4" };
        db.Members.AddRange(mA, mB, mC, mD);
        await db.SaveChangesAsync();
        // SaveChanges 会把 CreatedAt 盖成 now；A/B/C 改回 6 个月前（修改态不会再被覆盖），D 保留本月
        mA.CreatedAt = old;
        mB.CreatedAt = old;
        mC.CreatedAt = old;
        await db.SaveChangesAsync();

        db.Orders.Add(CompletedOrder(1, 1, now.AddDays(-5)));
        db.Orders.Add(CompletedOrder(2, 1, now.AddDays(-40)));   // A 第二单 → 复购
        db.Orders.Add(CompletedOrder(3, 2, now.AddDays(-50)));   // B 沉睡
        db.Orders.Add(CompletedOrder(4, 3, now.AddDays(-120)));  // C 流失
        await db.SaveChangesAsync();

        var dto = ((await c.MemberAnalysis(1, default)).Result as OkObjectResult)!.Value as MemberAnalysisDto;

        dto!.TotalMembers.Should().Be(4);
        dto.ActiveMembers.Should().Be(1);
        dto.DormantMembers.Should().Be(1);
        dto.LostMembers.Should().Be(1);
        dto.NeverConsumed.Should().Be(1);
        dto.NewMembersThisMonth.Should().Be(1);
        dto.RepeatMembers.Should().Be(1);
        dto.RepeatRate.Should().Be(33.3m); // 1 / 3 消费过的会员
    }

    [Fact]
    public async Task ServiceTrend_aggregates_monthly_rounds()
    {
        var db = NewDb(out var c);
        var now = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 15, 10, 0, 0, DateTimeKind.Utc);
        var twoMonthsAgo = thisMonth.AddMonths(-2);

        var o1 = CompletedOrder(1, null, thisMonth);
        var o2 = CompletedOrder(2, null, twoMonthsAgo);
        db.Orders.AddRange(o1, o2);
        db.OrderItems.Add(new OrderItem { Id = 1, TenantId = 1, OrderId = 1, Order = o1, ServiceId = 10, ServiceName = "推拿", TechnicianId = 1, Quantity = 3, ItemTotal = 300m });
        db.OrderItems.Add(new OrderItem { Id = 2, TenantId = 1, OrderId = 2, Order = o2, ServiceId = 10, ServiceName = "推拿", TechnicianId = 1, Quantity = 2, ItemTotal = 200m });
        db.OrderItems.Add(new OrderItem { Id = 3, TenantId = 1, OrderId = 1, Order = o1, ServiceId = 11, ServiceName = "足疗", TechnicianId = 1, Quantity = 1, ItemTotal = 88m });
        await db.SaveChangesAsync();

        var dto = ((await c.ServiceTrend(1, 6, default)).Result as OkObjectResult)!.Value as ServicePopularityTrendDto;

        dto!.Months.Should().Be(6);
        var tuina = dto.Services.First(s => s.ServiceName == "推拿");
        tuina.TotalRounds.Should().Be(5);
        tuina.Months.Should().HaveCount(6);
        tuina.Months[^1].Rounds.Should().Be(3, "最后一个桶是当月");
    }

    [Fact]
    public async Task TechnicianQuality_computes_complaint_rate()
    {
        var db = NewDb(out var c);
        var now = DateTime.UtcNow;
        var from = now.AddDays(-30);
        var to = now.AddDays(1);

        db.Users.Add(new User { Id = 1, TenantId = 1, StoreId = 1, Username = "techA", PasswordHash = "x", RealName = "甲", Role = UserRole.Technician });
        db.Users.Add(new User { Id = 2, TenantId = 1, StoreId = 1, Username = "techB", PasswordHash = "x", RealName = "乙", Role = UserRole.Technician });

        var o = CompletedOrder(1, null, now.AddDays(-3));
        db.Orders.Add(o);
        db.OrderItems.Add(new OrderItem { Id = 1, TenantId = 1, OrderId = 1, Order = o, ServiceId = 10, ServiceName = "推拿", TechnicianId = 1, Quantity = 6, ItemTotal = 1m });
        db.OrderItems.Add(new OrderItem { Id = 2, TenantId = 1, OrderId = 1, Order = o, ServiceId = 10, ServiceName = "推拿", TechnicianId = 1, Quantity = 4, ItemTotal = 1m });
        db.OrderItems.Add(new OrderItem { Id = 3, TenantId = 1, OrderId = 1, Order = o, ServiceId = 11, ServiceName = "足疗", TechnicianId = 2, Quantity = 5, ItemTotal = 1m });

        // 甲：2 条有效投诉 + 1 条已撤销（不计）
        db.ServiceComplaints.Add(new ServiceComplaint { Id = 1, TenantId = 1, StoreId = 1, OrderId = 1, OrderItemId = 1, OriginalTechnicianId = 1, Status = ComplaintStatus.Pending, CreatedAt = now.AddDays(-2) });
        db.ServiceComplaints.Add(new ServiceComplaint { Id = 2, TenantId = 1, StoreId = 1, OrderId = 1, OrderItemId = 2, OriginalTechnicianId = 1, Status = ComplaintStatus.Resolved, CreatedAt = now.AddDays(-1) });
        db.ServiceComplaints.Add(new ServiceComplaint { Id = 3, TenantId = 1, StoreId = 1, OrderId = 1, OrderItemId = 1, OriginalTechnicianId = 1, Status = ComplaintStatus.Cancelled, CreatedAt = now.AddDays(-1) });
        await db.SaveChangesAsync();

        var list = ((await c.TechnicianQuality(1, from, to, default)).Result as OkObjectResult)!
            .Value as IReadOnlyList<TechnicianQualityDto>;

        list.Should().HaveCount(2);
        var jia = list!.First(x => x.TechnicianName == "甲");
        jia.RoundCount.Should().Be(10);
        jia.ComplaintCount.Should().Be(2, "已撤销投诉不计");
        jia.ComplaintRate.Should().Be(20m);
        var yi = list!.First(x => x.TechnicianName == "乙");
        yi.ComplaintRate.Should().Be(0m);
        list![0].TechnicianName.Should().Be("甲", "按投诉率降序");
    }
}
