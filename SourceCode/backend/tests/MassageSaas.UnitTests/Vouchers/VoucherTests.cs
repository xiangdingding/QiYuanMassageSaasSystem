using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Orders;
using MassageSaas.Shared.Vouchers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace MassageSaas.UnitTests.Vouchers;

public class VoucherTests
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
        return (db, ctx);
    }

    private static VouchersController NewCtrl(ApplicationDbContext db, TenantContext ctx)
        => new(db, ctx, NullLogger<VouchersController>.Instance);

    private static CreateVoucherRequest Req(
        decimal face = 0, decimal? percent = null,
        string code = "V001", string title = "Test", decimal minAmount = 0,
        DateTime? validFrom = null, DateTime? expiresAt = null)
        => new("StoreCoupon", code, title, face, minAmount, percent, validFrom, expiresAt, null, null);

    [Fact]
    public async Task Create_Rejects_Both_FaceValue_And_DiscountPercent()
    {
        var (db, ctx) = NewDb();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.Create(Req(face: 20m, percent: 0.9m), default);

        var bad = result.Result as BadRequestObjectResult;
        bad.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_Rejects_When_Neither_Discount_Set()
    {
        var (db, ctx) = NewDb();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.Create(Req(face: 0m, percent: null), default);

        var bad = result.Result as BadRequestObjectResult;
        bad.Should().NotBeNull();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1.5)]
    public async Task Create_Rejects_Percent_Out_Of_Range(double percent)
    {
        var (db, ctx) = NewDb();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.Create(Req(percent: (decimal)percent), default);

        // 0 与 1 走 "neither / both" 分支也算拒绝；其它越界走 "InvalidDiscount" 分支
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_Accepts_FaceValue_Only()
    {
        var (db, ctx) = NewDb();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.Create(Req(face: 30m), default);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        (ok!.Value as VoucherDto)!.FaceValue.Should().Be(30m);
    }

    [Fact]
    public async Task Create_Accepts_DiscountPercent_Only()
    {
        var (db, ctx) = NewDb();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.Create(Req(percent: 0.88m), default);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        (ok!.Value as VoucherDto)!.DiscountPercent.Should().Be(0.88m);
    }

    [Fact]
    public async Task GetByCode_Returns_Voucher()
    {
        var (db, ctx) = NewDb();
        db.Vouchers.Add(new Voucher
        {
            TenantId = 1, Kind = VoucherKind.StoreCoupon, Code = "ABC",
            Title = "测试", FaceValue = 20m, Status = VoucherStatus.Active
        });
        await db.SaveChangesAsync();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.GetByCode("ABC", default);

        var ok = result.Result as OkObjectResult;
        (ok!.Value as VoucherDto)!.Code.Should().Be("ABC");
    }

    [Fact]
    public async Task GetByCode_NotFound_When_Missing()
    {
        var (db, ctx) = NewDb();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.GetByCode("MISSING", default);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static (Order Order, Store Store) SeedOrder(ApplicationDbContext db, decimal total = 100m)
    {
        var store = new Store { Id = 1, TenantId = 1, Name = "总店", IsActive = true };
        db.Stores.Add(store);
        var order = new Order
        {
            Id = 5001, TenantId = 1, StoreId = 1, OrderNo = "O001",
            Total = total, Status = OrderStatus.Pending, CreatedAt = DateTime.UtcNow
        };
        db.Orders.Add(order);
        db.SaveChanges();
        return (order, store);
    }

    [Fact]
    public async Task Redeem_HappyPath_TiesVoucherToOrder_AndFlipsStatus()
    {
        var (db, ctx) = NewDb();
        var (order, _) = SeedOrder(db, total: 100m);
        db.Vouchers.Add(new Voucher
        {
            Id = 7001, TenantId = 1, Kind = VoucherKind.StoreCoupon, Code = "REDEEM-OK",
            Title = "T", FaceValue = 30m, Status = VoucherStatus.Active
        });
        await db.SaveChangesAsync();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.Redeem(new VoucherRedeemRequest("REDEEM-OK", order.Id), default);

        (result.Result as OkObjectResult).Should().NotBeNull();
        var v = await db.Vouchers.AsNoTracking().FirstAsync(x => x.Id == 7001);
        v.Status.Should().Be(VoucherStatus.Redeemed);
        v.RedeemedOrderId.Should().Be(order.Id);
        var o = await db.Orders.AsNoTracking().FirstAsync(x => x.Id == order.Id);
        o.VoucherId.Should().Be(7001);
    }

    [Fact]
    public async Task Redeem_Rejects_Already_Attached_Order()
    {
        var (db, ctx) = NewDb();
        var (order, _) = SeedOrder(db);
        order.VoucherId = 9999;
        db.Vouchers.Add(new Voucher
        {
            TenantId = 1, Kind = VoucherKind.StoreCoupon, Code = "DOUBLE",
            Title = "T", FaceValue = 10m, Status = VoucherStatus.Active
        });
        await db.SaveChangesAsync();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.Redeem(new VoucherRedeemRequest("DOUBLE", order.Id), default);

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Redeem_Rejects_Below_MinOrderAmount()
    {
        var (db, ctx) = NewDb();
        var (order, _) = SeedOrder(db, total: 50m);
        db.Vouchers.Add(new Voucher
        {
            TenantId = 1, Kind = VoucherKind.StoreCoupon, Code = "MIN",
            Title = "T", FaceValue = 30m, MinOrderAmount = 100m, Status = VoucherStatus.Active
        });
        await db.SaveChangesAsync();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.Redeem(new VoucherRedeemRequest("MIN", order.Id), default);

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Redeem_Rejects_Expired_And_AutoMarks_Expired()
    {
        var (db, ctx) = NewDb();
        var (order, _) = SeedOrder(db);
        db.Vouchers.Add(new Voucher
        {
            Id = 7100, TenantId = 1, Kind = VoucherKind.StoreCoupon, Code = "EXP",
            Title = "T", FaceValue = 30m, Status = VoucherStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.Redeem(new VoucherRedeemRequest("EXP", order.Id), default);

        result.Result.Should().BeOfType<ConflictObjectResult>();
        var v = await db.Vouchers.AsNoTracking().FirstAsync(x => x.Id == 7100);
        v.Status.Should().Be(VoucherStatus.Expired, "过期券核销时自动改写状态");
    }

    private static BatchCreateVoucherRequest BatchReq(int count, decimal face = 100m, decimal? percent = null,
        string kind = "GroupBuy", string title = "团购 100 元券", decimal minAmount = 100m)
        => new(kind, count, title, face, minAmount, percent, null, null, null, null);

    [Fact]
    public async Task BatchCreate_Generates_N_Unique_Codes_With_Same_Spec()
    {
        var (db, ctx) = NewDb();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.BatchCreate(BatchReq(count: 100), default);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var resp = ok!.Value as BatchCreateVoucherResponse;
        resp!.Created.Should().Be(100);
        resp.Codes.Should().HaveCount(100);
        resp.Codes.Distinct().Should().HaveCount(100, "批内必须全部唯一");
        resp.Codes.Should().AllSatisfy(c => c.Should().StartWith("GB-"));

        var rows = await db.Vouchers.AsNoTracking().ToListAsync();
        rows.Should().HaveCount(100);
        rows.Should().AllSatisfy(v =>
        {
            v.FaceValue.Should().Be(100m);
            v.MinOrderAmount.Should().Be(100m);
            v.Kind.Should().Be(VoucherKind.GroupBuy);
            v.Status.Should().Be(VoucherStatus.Active);
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public async Task BatchCreate_Rejects_Out_Of_Range_Count(int count)
    {
        var (db, ctx) = NewDb();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.BatchCreate(BatchReq(count), default);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BatchCreate_Rejects_Both_FaceValue_And_Percent()
    {
        var (db, ctx) = NewDb();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.BatchCreate(BatchReq(count: 10, face: 50m, percent: 0.9m), default);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BatchCreate_StoreCoupon_Prefix_Is_SC()
    {
        var (db, ctx) = NewDb();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.BatchCreate(BatchReq(count: 3, kind: "StoreCoupon"), default);

        var resp = (result.Result as OkObjectResult)!.Value as BatchCreateVoucherResponse;
        resp!.Codes.Should().AllSatisfy(c => c.Should().StartWith("SC-"));
    }

    [Fact]
    public async Task BatchCreate_Avoids_Collision_With_Existing_Code()
    {
        // 提前埋一张码，模拟 GenerateCode 撞库后端走"补一轮新码"分支。
        var (db, ctx) = NewDb();
        db.Vouchers.Add(new Voucher
        {
            TenantId = 1, Kind = VoucherKind.GroupBuy, Code = "GB-AAAA-AAAA",
            Title = "已有券", FaceValue = 10m, Status = VoucherStatus.Active
        });
        await db.SaveChangesAsync();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.BatchCreate(BatchReq(count: 50), default);

        var resp = (result.Result as OkObjectResult)!.Value as BatchCreateVoucherResponse;
        resp!.Codes.Should().HaveCount(50);
        resp.Codes.Should().NotContain("GB-AAAA-AAAA");
        var total = await db.Vouchers.CountAsync();
        total.Should().Be(51, "批量 50 + 已有 1");
    }

    private static Voucher V(long id, VoucherStatus status, string code)
        => new()
        {
            Id = id, TenantId = 1, Kind = VoucherKind.StoreCoupon, Code = code,
            Title = "T", FaceValue = 10m, Status = status
        };

    [Fact]
    public async Task BulkCancel_Affects_Only_Active_And_Skips_Others_With_Reasons()
    {
        var (db, ctx) = NewDb();
        db.Vouchers.AddRange(
            V(1, VoucherStatus.Active, "A1"),
            V(2, VoucherStatus.Active, "A2"),
            V(3, VoucherStatus.Redeemed, "R3"),
            V(4, VoucherStatus.Cancelled, "C4"),
            V(5, VoucherStatus.Expired, "E5")
        );
        await db.SaveChangesAsync();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.BulkCancel(
            new BulkVoucherActionRequest(new long[] { 1, 2, 3, 4, 5, 999 }), default);

        var resp = (result.Result as OkObjectResult)!.Value as BulkVoucherActionResponse;
        resp!.Affected.Should().Be(2);
        resp.Skipped.Should().HaveCount(4);
        resp.Skipped.Select(s => s.Id).Should().BeEquivalentTo(new long[] { 3, 4, 5, 999 });

        var rows = await db.Vouchers.AsNoTracking().OrderBy(v => v.Id).ToListAsync();
        rows.Single(v => v.Id == 1).Status.Should().Be(VoucherStatus.Cancelled);
        rows.Single(v => v.Id == 2).Status.Should().Be(VoucherStatus.Cancelled);
        rows.Single(v => v.Id == 3).Status.Should().Be(VoucherStatus.Redeemed, "已核销不可作废");
        rows.Single(v => v.Id == 5).Status.Should().Be(VoucherStatus.Expired, "过期不可作废");
    }

    [Fact]
    public async Task BulkDelete_Removes_Only_Cancelled_And_Skips_Others()
    {
        var (db, ctx) = NewDb();
        db.Vouchers.AddRange(
            V(10, VoucherStatus.Cancelled, "X10"),
            V(11, VoucherStatus.Cancelled, "X11"),
            V(12, VoucherStatus.Active, "X12"),
            V(13, VoucherStatus.Redeemed, "X13"),
            V(14, VoucherStatus.Expired, "X14")
        );
        await db.SaveChangesAsync();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.BulkDelete(
            new BulkVoucherActionRequest(new long[] { 10, 11, 12, 13, 14, 888 }), default);

        var resp = (result.Result as OkObjectResult)!.Value as BulkVoucherActionResponse;
        resp!.Affected.Should().Be(2);
        resp.Skipped.Should().HaveCount(4);

        var remaining = await db.Vouchers.AsNoTracking().Select(v => v.Id).OrderBy(x => x).ToListAsync();
        remaining.Should().BeEquivalentTo(new long[] { 12, 13, 14 }, "Active / Redeemed / Expired 都没动");
    }

    [Fact]
    public async Task BulkCancel_Rejects_Empty_Ids()
    {
        var (db, ctx) = NewDb();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.BulkCancel(new BulkVoucherActionRequest(Array.Empty<long>()), default);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BulkDelete_Rejects_Empty_Ids()
    {
        var (db, ctx) = NewDb();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.BulkDelete(new BulkVoucherActionRequest(Array.Empty<long>()), default);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BulkCancel_Rejects_Too_Many_Ids()
    {
        var (db, ctx) = NewDb();
        var ctl = NewCtrl(db, ctx);

        var result = await ctl.BulkCancel(new BulkVoucherActionRequest(Enumerable.Range(1, 501).Select(i => (long)i).ToList()), default);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Reopen_Restores_Voucher_To_Active_And_Clears_OrderBinding()
    {
        // 完整跑 OrdersController.Reopen 的副作用：order.VoucherId 清空 + voucher 回 Active。
        var (db, ctx) = NewDb();
        var store = new Store { Id = 1, TenantId = 1, Name = "总店", IsActive = true };
        db.Stores.Add(store);
        db.Vouchers.Add(new Voucher
        {
            Id = 8001, TenantId = 1, Kind = VoucherKind.StoreCoupon, Code = "BACK",
            Title = "T", FaceValue = 20m, Status = VoucherStatus.Redeemed,
            RedeemedAt = DateTime.UtcNow.AddMinutes(-5), RedeemedOrderId = 9001
        });
        db.Orders.Add(new Order
        {
            Id = 9001, TenantId = 1, StoreId = 1, OrderNo = "O-BACK",
            Total = 200m, PaidAmount = 180m, DiscountAmount = 20m,
            PayMethod = PayMethod.Cash, Status = OrderStatus.Completed,
            CompletedAt = DateTime.UtcNow.AddMinutes(-4),
            VoucherId = 8001
        });
        await db.SaveChangesAsync();

        var ctl = new OrdersController(db, ctx, NullLogger<OrdersController>.Instance);
        var result = await ctl.Reopen(9001, new ReopenOrderRequest("误结账"), default);

        (result.Result as OkObjectResult).Should().NotBeNull();
        var v = await db.Vouchers.AsNoTracking().FirstAsync(x => x.Id == 8001);
        v.Status.Should().Be(VoucherStatus.Active);
        v.RedeemedOrderId.Should().BeNull();
        v.RedeemedAt.Should().BeNull();
        var o = await db.Orders.AsNoTracking().FirstAsync(x => x.Id == 9001);
        o.VoucherId.Should().BeNull();
        o.Status.Should().Be(OrderStatus.InProgress);
    }
}
