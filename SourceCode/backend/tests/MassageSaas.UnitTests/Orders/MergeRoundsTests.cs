using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace MassageSaas.UnitTests.Orders;

public class MergeRoundsTests
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

    /// <summary>造一张订单 + 一个 item，返回 itemId。</summary>
    private static long SeedItem(ApplicationDbContext db, long techId, OrderStatus status = OrderStatus.InProgress)
    {
        var orderId = Random.Shared.NextInt64(1_000_000, 9_000_000);
        var order = new Order
        {
            Id = orderId, TenantId = 1, StoreId = 1,
            OrderNo = $"O{orderId}", Status = status, CreatedAt = DateTime.UtcNow
        };
        db.Orders.Add(order);
        var item = new OrderItem
        {
            TenantId = 1, OrderId = orderId, Order = order,
            ServiceId = 1, ServiceName = "60min", TechnicianId = techId,
            DurationMinutes = 60, UnitPrice = 100m, Quantity = 1, ItemTotal = 100m
        };
        db.OrderItems.Add(item);
        db.SaveChanges();
        return item.Id;
    }

    private static OrdersController NewController(ApplicationDbContext db, TenantContext ctx) =>
        new(db, ctx, NullLogger<OrdersController>.Instance);

    [Fact]
    public async Task Merge_AssignsSharedGroupKey()
    {
        var (db, ctx) = NewDb();
        var a = SeedItem(db, techId: 10);
        var b = SeedItem(db, techId: 10);
        var ctl = NewController(db, ctx);

        var resp = await ctl.MergeItems(new MergeOrderItemsRequest(new[] { a, b }), default);
        (resp.Result as OkObjectResult).Should().NotBeNull();

        var items = await db.OrderItems.AsNoTracking().Where(i => i.Id == a || i.Id == b).ToListAsync();
        items.Select(i => i.MergedGroupKey).Distinct().Should().HaveCount(1);
        items[0].MergedGroupKey.Should().NotBeNull();
    }

    [Fact]
    public async Task Merge_RejectsFewerThanTwo()
    {
        var (db, ctx) = NewDb();
        var a = SeedItem(db, techId: 10);
        var ctl = NewController(db, ctx);
        var resp = (await ctl.MergeItems(new MergeOrderItemsRequest(new[] { a }), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Merge_RejectsDifferentTechnicians()
    {
        var (db, ctx) = NewDb();
        var a = SeedItem(db, techId: 10);
        var b = SeedItem(db, techId: 11);
        var ctl = NewController(db, ctx);
        var resp = (await ctl.MergeItems(new MergeOrderItemsRequest(new[] { a, b }), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Merge_RejectsCheckedOutOrder()
    {
        var (db, ctx) = NewDb();
        var a = SeedItem(db, techId: 10);
        var b = SeedItem(db, techId: 10, status: OrderStatus.Completed);
        var ctl = NewController(db, ctx);
        var resp = (await ctl.MergeItems(new MergeOrderItemsRequest(new[] { a, b }), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Merge_RejectsAlreadyMergedItem()
    {
        var (db, ctx) = NewDb();
        var a = SeedItem(db, techId: 10);
        var b = SeedItem(db, techId: 10);
        var c = SeedItem(db, techId: 10);
        var ctl = NewController(db, ctx);
        await ctl.MergeItems(new MergeOrderItemsRequest(new[] { a, b }), default);

        var resp = (await ctl.MergeItems(new MergeOrderItemsRequest(new[] { a, c }), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Unmerge_ClearsGroup_AndDropsOrphan()
    {
        var (db, ctx) = NewDb();
        var a = SeedItem(db, techId: 10);
        var b = SeedItem(db, techId: 10);
        var ctl = NewController(db, ctx);
        await ctl.MergeItems(new MergeOrderItemsRequest(new[] { a, b }), default);

        var result = await ctl.UnmergeItem(a, default);
        (result as NoContentResult).Should().NotBeNull();

        // a 被清除；b 是组内仅剩的一项，应被连带清除以免孤立
        var items = await db.OrderItems.AsNoTracking().Where(i => i.Id == a || i.Id == b).ToListAsync();
        items.Should().OnlyContain(i => i.MergedGroupKey == null);
    }

    [Fact]
    public async Task Unmerge_KeepsGroupWhenThreePlus()
    {
        var (db, ctx) = NewDb();
        var a = SeedItem(db, techId: 10);
        var b = SeedItem(db, techId: 10);
        var c = SeedItem(db, techId: 10);
        var ctl = NewController(db, ctx);
        await ctl.MergeItems(new MergeOrderItemsRequest(new[] { a, b, c }), default);

        await ctl.UnmergeItem(a, default);

        var rest = await db.OrderItems.AsNoTracking().Where(i => i.Id == b || i.Id == c).ToListAsync();
        rest.Select(i => i.MergedGroupKey).Distinct().Should().HaveCount(1);
        rest[0].MergedGroupKey.Should().NotBeNull("组内还有 2 项，保留并钟");
    }
}
