using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Tenants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace MassageSaas.UnitTests.Tenants;

public class TenantOverviewTests
{
    private static TenantsController NewController()
    {
        var ctx = new TenantContext();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"overview_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();

        db.Plans.Add(new Plan { Id = 1, Code = "STD", Name = "标准版", AnnualPrice = 2000m, IsActive = true });
        db.Tenants.Add(new Tenant
        {
            Id = 1, Name = "康乐按摩", ContactPhone = "13800000000",
            Status = TenantStatus.Active, CurrentPlanId = 1, ExpireAt = DateTime.UtcNow.AddDays(100)
        });
        db.Stores.Add(new Store { Id = 1, TenantId = 1, Name = "总店", IsActive = true });
        db.Stores.Add(new Store { Id = 2, TenantId = 1, Name = "分店（停用）", IsActive = false });

        db.Users.Add(new User
        {
            Id = 10, TenantId = 1, StoreId = 1, Username = "techA", PasswordHash = "x",
            RealName = "甲师傅", Role = UserRole.Technician, IsActive = true
        });
        db.Users.Add(new User
        {
            Id = 11, TenantId = 1, StoreId = 1, Username = "techB", PasswordHash = "x",
            RealName = "乙师傅", Role = UserRole.Technician, IsActive = true
        });
        db.Users.Add(new User
        {
            Id = 12, TenantId = 1, StoreId = 1, Username = "owner", PasswordHash = "x",
            RealName = "店主", Role = UserRole.ShopOwner, IsActive = true
        });

        db.Members.Add(new Member { Id = 100, TenantId = 1, StoreId = 1, CardNo = "M1", Phone = "13900000001" });
        db.Members.Add(new Member { Id = 101, TenantId = 1, StoreId = 1, CardNo = "M2", Phone = "13900000002" });

        var now = DateTime.UtcNow;
        var order1 = new Order
        {
            Id = 1, TenantId = 1, StoreId = 1, OrderNo = "O1",
            Status = OrderStatus.Completed, CompletedAt = now.AddDays(-2), PaidAmount = 300m
        };
        var order2 = new Order
        {
            Id = 2, TenantId = 1, StoreId = 1, OrderNo = "O2",
            Status = OrderStatus.Completed, CompletedAt = now.AddDays(-20), PaidAmount = 150m
        };
        var order3 = new Order
        {
            Id = 3, TenantId = 1, StoreId = 1, OrderNo = "O3",
            Status = OrderStatus.Pending, PaidAmount = 999m
        };
        db.Orders.AddRange(order1, order2, order3);
        db.OrderItems.AddRange(
            new OrderItem { Id = 1, TenantId = 1, OrderId = 1, Order = order1, ServiceId = 1, TechnicianId = 10, ServiceName = "全身", ItemTotal = 200m },
            new OrderItem { Id = 2, TenantId = 1, OrderId = 1, Order = order1, ServiceId = 1, TechnicianId = 11, ServiceName = "足疗", ItemTotal = 100m },
            new OrderItem { Id = 3, TenantId = 1, OrderId = 2, Order = order2, ServiceId = 1, TechnicianId = 10, ServiceName = "全身", ItemTotal = 150m });
        db.SaveChanges();

        return new TenantsController(db, ctx, NullLogger<TenantsController>.Instance);
    }

    [Fact]
    public async Task Overview_returns_NotFound_for_unknown_tenant()
    {
        var result = await NewController().Overview(999, default);
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Overview_reports_scale_counts()
    {
        var dto = ((await NewController().Overview(1, default)).Result as OkObjectResult)!
            .Value as TenantOverviewDto;

        dto!.StoreCount.Should().Be(2);
        dto.ActiveStoreCount.Should().Be(1);
        dto.StaffCount.Should().Be(3);
        dto.TechnicianCount.Should().Be(2);
        dto.MemberCount.Should().Be(2);
        dto.CurrentPlanName.Should().Be("标准版");
    }

    [Fact]
    public async Task Overview_revenue_windows_count_only_completed_orders()
    {
        var dto = ((await NewController().Overview(1, default)).Result as OkObjectResult)!
            .Value as TenantOverviewDto;

        dto!.Revenue7Days.Should().Be(300m, "20 天前那单不在 7 天窗口内");
        dto.Revenue30Days.Should().Be(450m);
        dto.OrderCount30Days.Should().Be(2, "待支付订单不计入");
    }

    [Fact]
    public async Task Overview_ranks_top_technicians_by_revenue()
    {
        var dto = ((await NewController().Overview(1, default)).Result as OkObjectResult)!
            .Value as TenantOverviewDto;

        dto!.TopTechnicians.Should().HaveCount(2);
        dto.TopTechnicians[0].Name.Should().Be("甲师傅");
        dto.TopTechnicians[0].Revenue.Should().Be(350m);
        dto.TopTechnicians[0].RoundCount.Should().Be(2);
        dto.TopTechnicians[1].Name.Should().Be("乙师傅");
    }
}
