using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MassageSaas.UnitTests.Dashboard;

public class PlatformRevenueTests
{
    private static DashboardController NewController()
    {
        var ctx = new TenantContext();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"revenue_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();

        var basic = new Plan { Id = 1, Code = "BASIC", Name = "基础版", AnnualPrice = 2000m, IsActive = true };
        var pro = new Plan { Id = 2, Code = "PRO", Name = "旗舰版", AnnualPrice = 3000m, IsActive = true };
        db.Plans.AddRange(basic, pro);
        db.Tenants.Add(new Tenant { Id = 1, Name = "甲店", ContactPhone = "13800000001", Status = TenantStatus.Active });
        db.Tenants.Add(new Tenant { Id = 2, Name = "乙店", ContactPhone = "13800000002", Status = TenantStatus.Active });

        var now = DateTime.UtcNow;
        // 甲店：首笔（新签）+ 次笔（续费）
        db.PaymentOrders.Add(new PaymentOrder
        {
            Id = 1, TenantId = 1, OrderNo = "P1", Amount = 2000m, PlanId = 1,
            Channel = PaymentChannel.Wechat, Status = PaymentStatus.Paid, PaidAt = now.AddMonths(-10), Years = 1
        });
        db.PaymentOrders.Add(new PaymentOrder
        {
            Id = 2, TenantId = 1, OrderNo = "P2", Amount = 2000m, PlanId = 1,
            Channel = PaymentChannel.Alipay, Status = PaymentStatus.Paid, PaidAt = now.AddDays(-5), Years = 1
        });
        // 乙店：首笔（新签）
        db.PaymentOrders.Add(new PaymentOrder
        {
            Id = 3, TenantId = 2, OrderNo = "P3", Amount = 3000m, PlanId = 2,
            Channel = PaymentChannel.Offline, Status = PaymentStatus.Paid, PaidAt = now.AddDays(-3), Years = 1
        });
        // 未支付，不计入
        db.PaymentOrders.Add(new PaymentOrder
        {
            Id = 4, TenantId = 2, OrderNo = "P4", Amount = 9999m, PlanId = 2,
            Channel = PaymentChannel.Wechat, Status = PaymentStatus.Pending, Years = 1
        });
        db.SaveChanges();
        return new DashboardController(db, ctx);
    }

    [Fact]
    public async Task Revenue_totals_only_paid_orders()
    {
        var dto = ((await NewController().Revenue(12, default)).Result as OkObjectResult)!
            .Value as PlatformRevenueDto;

        dto!.TotalOrders.Should().Be(3, "未支付订单不计入");
        dto.TotalAmount.Should().Be(7000m);
    }

    [Fact]
    public async Task Revenue_splits_new_and_renewal()
    {
        var dto = ((await NewController().Revenue(12, default)).Result as OkObjectResult)!
            .Value as PlatformRevenueDto;

        // 甲店首笔 2000 + 乙店首笔 3000 = 新签 5000；甲店次笔 2000 = 续费
        dto!.NewCustomerAmount.Should().Be(5000m);
        dto.RenewalAmount.Should().Be(2000m);
    }

    [Fact]
    public async Task Revenue_monthly_trend_covers_requested_window()
    {
        var dto = ((await NewController().Revenue(6, default)).Result as OkObjectResult)!
            .Value as PlatformRevenueDto;

        dto!.Months.Should().Be(6);
        dto.MonthlyTrend.Should().HaveCount(6);
        // 10 个月前那笔超出 6 个月窗口，应不在总额内
        dto.TotalAmount.Should().Be(5000m);
    }

    [Fact]
    public async Task Revenue_breaks_down_by_plan_and_channel()
    {
        var dto = ((await NewController().Revenue(12, default)).Result as OkObjectResult)!
            .Value as PlatformRevenueDto;

        dto!.ByPlan.Should().Contain(b => b.Name == "基础版" && b.Amount == 4000m);
        dto.ByPlan.Should().Contain(b => b.Name == "旗舰版" && b.Amount == 3000m);
        dto.ByChannel.Should().Contain(b => b.Name == "Offline" && b.Amount == 3000m);
    }
}
