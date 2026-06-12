using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Payroll;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace MassageSaas.UnitTests.Payroll;

public class PayrollTests
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

    private static User AddTech(ApplicationDbContext db, long id, int empNo = 1)
    {
        var u = new User
        {
            Id = id, TenantId = 1, StoreId = 1,
            Username = $"t{id}", PasswordHash = "x",
            Role = UserRole.Technician, IsActive = true, EmployeeNo = empNo
        };
        db.Users.Add(u);
        return u;
    }

    private static void AddCompletedOrderItem(
        ApplicationDbContext db,
        long techId,
        DateTime completedAt,
        decimal commission,
        decimal tip = 0m,
        int qty = 1)
    {
        var orderId = Random.Shared.NextInt64(1_000_000, 9_000_000);
        var order = new Order
        {
            Id = orderId,
            TenantId = 1, StoreId = 1, OrderNo = $"O{orderId}",
            Status = OrderStatus.Completed,
            PayMethod = PayMethod.Cash,
            CompletedAt = completedAt,
            Total = 100m * qty, PaidAmount = 100m * qty,
            StartedAt = completedAt.AddHours(-1)
        };
        db.Orders.Add(order);
        db.OrderItems.Add(new OrderItem
        {
            TenantId = 1, OrderId = orderId, Order = order,
            ServiceId = 1, ServiceName = "60min",
            TechnicianId = techId,
            DurationMinutes = 60, UnitPrice = 100m, Quantity = qty,
            ItemTotal = 100m * qty,
            CommissionAmount = commission,
            TipAmount = tip
        });
    }

    private static PayrollController NewController(ApplicationDbContext db, TenantContext ctx) =>
        new(db, ctx, NullLogger<PayrollController>.Instance);

    [Fact]
    public async Task Generate_AggregatesCommissionAndTipsInMonth()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10);
        // 在 2026-04 月内（2 单提成 + 1 笔小费），月外 1 单（应忽略）
        AddCompletedOrderItem(db, 10, new DateTime(2026, 4, 5, 14, 0, 0, DateTimeKind.Utc), commission: 30m, tip: 5m);
        AddCompletedOrderItem(db, 10, new DateTime(2026, 4, 20, 18, 0, 0, DateTimeKind.Utc), commission: 20m);
        AddCompletedOrderItem(db, 10, new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc), commission: 999m); // 月外
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var ok = (await ctl.Generate(new GeneratePayrollRequest(1, 2026, 4, null), default)).Result as OkObjectResult;
        var detail = ok!.Value as PayrollPeriodDetailDto;

        var item = detail!.Items.Single(i => i.UserId == 10);
        item.CommissionTotal.Should().Be(50m);
        item.TipsTotal.Should().Be(5m, "tips 不进 NetTotal，但要在详情里展示");
        item.BaseSalary.Should().Be(0m, "没设 SalaryProfile");
        item.NetTotal.Should().Be(50m, "底薪 0 + 提成 50 + 加班 0 + 满勤 0");
        detail.Period.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task Generate_AddsBaseSalaryFromProfile()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10);
        db.SalaryProfiles.Add(new SalaryProfile
        {
            TenantId = 1, UserId = 10, BaseMonthly = 3000m,
            OvertimeHourRate = 50m, AttendanceBonusAmount = 200m, RequiredAttendanceDays = 22
        });
        AddCompletedOrderItem(db, 10, new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc), commission: 80m);
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var ok = (await ctl.Generate(new GeneratePayrollRequest(1, 2026, 4, null), default)).Result as OkObjectResult;
        var detail = ok!.Value as PayrollPeriodDetailDto;

        var item = detail!.Items.Single();
        item.BaseSalary.Should().Be(3000m);
        item.CommissionTotal.Should().Be(80m);
        item.AttendanceBonus.Should().Be(0m, "ScheduledDays 不够 22 天");
        item.NetTotal.Should().Be(3080m);
    }

    [Fact]
    public async Task Generate_GivesAttendanceBonus_WhenScheduledMeetsThreshold()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10);
        db.SalaryProfiles.Add(new SalaryProfile
        {
            TenantId = 1, UserId = 10, BaseMonthly = 0m,
            AttendanceBonusAmount = 300m, RequiredAttendanceDays = 3
        });
        // 排 3 天
        for (var d = 1; d <= 3; d++)
        {
            db.StaffSchedules.Add(new StaffSchedule
            {
                TenantId = 1, StoreId = 1, UserId = 10,
                WorkDate = new DateOnly(2026, 4, d),
                StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(18, 0)
            });
        }
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var ok = (await ctl.Generate(new GeneratePayrollRequest(1, 2026, 4, null), default)).Result as OkObjectResult;
        var detail = ok!.Value as PayrollPeriodDetailDto;
        detail!.Items.Single().AttendanceBonus.Should().Be(300m);
    }

    [Fact]
    public async Task Generate_RejectsDuplicateMonthForSameStore()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10);
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        await ctl.Generate(new GeneratePayrollRequest(1, 2026, 4, null), default);
        var second = (await ctl.Generate(new GeneratePayrollRequest(1, 2026, 4, null), default)).Result as ObjectResult;
        second!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Generate_RejectsFutureMonth()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10);
        await db.SaveChangesAsync();

        // 下个月（业务月按北京时间）应被拒绝
        var nextCn = DateTime.UtcNow.AddHours(8).AddMonths(1);
        var ctl = NewController(db, ctx);
        var rejected = (await ctl.Generate(new GeneratePayrollRequest(1, nextCn.Year, nextCn.Month, null), default))
            .Result as ObjectResult;
        rejected!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Generate_AllowsCurrentMonth()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10);
        await db.SaveChangesAsync();

        var nowCn = DateTime.UtcNow.AddHours(8);
        var ctl = NewController(db, ctx);
        var ok = (await ctl.Generate(new GeneratePayrollRequest(1, nowCn.Year, nowCn.Month, null), default)).Result as OkObjectResult;
        ok.Should().NotBeNull("当前月允许生成");
    }

    [Fact]
    public async Task DeleteDraft_HardDeletes_AllowingSameMonthRegenerate()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10);
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var generated = ((await ctl.Generate(new GeneratePayrollRequest(1, 2026, 4, null), default)).Result as OkObjectResult)!
            .Value as PayrollPeriodDetailDto;
        var periodId = generated!.Period.Id;

        var del = await ctl.DeleteDraft(periodId, default);
        del.Should().BeOfType<NoContentResult>();

        // 物理删除：包含软删除过滤器在内都查不到，且不残留占用 UNIQUE(StoreId,Year,Month) 的行
        (await db.PayrollPeriods.IgnoreQueryFilters().AnyAsync(p => p.Id == periodId)).Should().BeFalse();

        // 同月可再次生成，不应再报 409 重复
        var regen = (await ctl.Generate(new GeneratePayrollRequest(1, 2026, 4, null), default)).Result as OkObjectResult;
        regen.Should().NotBeNull();
        (regen!.Value as PayrollPeriodDetailDto)!.Period.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task UpdateItem_AppliesOvertimeUsingProfileRate()
    {
        var (db, ctx) = NewDb();
        var user = AddTech(db, 10);
        db.SalaryProfiles.Add(new SalaryProfile
        {
            TenantId = 1, UserId = 10, BaseMonthly = 0m,
            OvertimeHourRate = 50m
        });
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var generated = ((await ctl.Generate(new GeneratePayrollRequest(1, 2026, 4, null), default)).Result as OkObjectResult)!
            .Value as PayrollPeriodDetailDto;
        var itemId = generated!.Items.Single().Id;

        var updated = ((await ctl.UpdateItem(itemId,
            new UpdatePayrollItemRequest(OvertimeHours: 8m, AttendanceBonusOverride: -1m, Remark: null),
            default)).Result as OkObjectResult)!.Value as PayrollItemDto;

        updated!.OvertimeHours.Should().Be(8m);
        updated.OvertimeAmount.Should().Be(400m, "8 × 50");
        updated.NetTotal.Should().Be(400m, "base 0 + commission 0 + overtime 400");
    }

    [Fact]
    public async Task AddAdjustment_BonusIncreasesNet_DeductionDecreasesNet()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10);
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var generated = ((await ctl.Generate(new GeneratePayrollRequest(1, 2026, 4, null), default)).Result as OkObjectResult)!
            .Value as PayrollPeriodDetailDto;
        var itemId = generated!.Items.Single().Id;

        var withBonus = ((await ctl.AddAdjustment(itemId, new AddAdjustmentRequest("Bonus", 200m, "推荐奖"), default))
            .Result as OkObjectResult)!.Value as PayrollItemDto;
        withBonus!.NetTotal.Should().Be(200m);

        var withDeduct = ((await ctl.AddAdjustment(itemId, new AddAdjustmentRequest("Deduction", 80m, "迟到"), default))
            .Result as OkObjectResult)!.Value as PayrollItemDto;
        withDeduct!.NetTotal.Should().Be(120m, "200 - 80");
        withDeduct.AdjustmentTotal.Should().Be(120m);
    }

    [Fact]
    public async Task Lock_PreventsFurtherEdits()
    {
        var (db, ctx) = NewDb();
        AddTech(db, 10);
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var generated = ((await ctl.Generate(new GeneratePayrollRequest(1, 2026, 4, null), default)).Result as OkObjectResult)!
            .Value as PayrollPeriodDetailDto;

        await ctl.Lock(generated!.Period.Id, new LockPayrollRequest(null), default);

        var rejected = (await ctl.UpdateItem(generated.Items.Single().Id,
            new UpdatePayrollItemRequest(OvertimeHours: 5m, AttendanceBonusOverride: -1m, Remark: null),
            default)).Result as ObjectResult;
        rejected!.StatusCode.Should().Be(409);
    }
}
