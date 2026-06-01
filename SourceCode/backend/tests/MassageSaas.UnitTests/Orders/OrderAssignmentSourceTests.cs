using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Application.Abstractions;
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

/// <summary>
/// OrderItem.AssignmentSource 端到端：建单时落库、缺失/无效时兜底、转钟保留、加钟透传、
/// Checkout 提成按 source-specific 规则算。
/// </summary>
public class OrderAssignmentSourceTests
{
    private static (ApplicationDbContext Db, TenantContext Ctx, Member Member, ServiceItem Service, User Tech)
        Seed()
    {
        var ctx = new TenantContext { TenantId = 1, UserId = 99 };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"db_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();

        db.Stores.Add(new Store { Id = 1, TenantId = 1, Name = "总店", IsActive = true });
        var tech = new User
        {
            Id = 10, TenantId = 1, StoreId = 1, Username = "t1",
            PasswordHash = "x", Role = UserRole.Technician, IsActive = true, EmployeeNo = 1
        };
        db.Users.Add(tech);
        var svc = new ServiceItem
        {
            Id = 100, TenantId = 1, Code = "S1", Name = "60 分钟",
            DurationMinutes = 60, Price = 200m, MemberPrice = 180m, IsActive = true
        };
        db.ServiceItems.Add(svc);
        var member = new Member
        {
            Id = 1000, TenantId = 1, StoreId = 1, CardNo = "C001",
            Phone = "13800138000", Balance = 1000m, Discount = 1.0m,
            TotalRecharge = 1000m, TotalConsumed = 0
        };
        db.Members.Add(member);
        db.SaveChanges();
        return (db, ctx, member, svc, tech);
    }

    private static OrdersController NewController(ApplicationDbContext db, TenantContext ctx) =>
        new(db, ctx, NullLogger<OrdersController>.Instance);

    [Fact]
    public async Task Create_WithRotationSource_PersistsAssignmentSource()
    {
        var (db, ctx, member, svc, tech) = Seed();
        var ctl = NewController(db, ctx);

        var result = (await ctl.Create(new CreateOrderRequest(
            StoreId: 1, MemberId: member.Id,
            Items: new[] { new OrderItemInputDto(svc.Id, tech.Id, AssignmentSource: "Rotation") },
            Remark: null), default)).Result as ObjectResult;

        var dto = result!.Value as OrderDto;
        dto!.Items.Single().AssignmentSource.Should().Be("Rotation");

        var item = await db.OrderItems.AsNoTracking().FirstAsync(i => i.OrderId == dto.Id);
        item.AssignmentSource.Should().Be(AssignmentSource.Rotation);
    }

    [Fact]
    public async Task Create_WithDesignationSource_PersistsAssignmentSource()
    {
        var (db, ctx, member, svc, tech) = Seed();
        var ctl = NewController(db, ctx);

        var result = (await ctl.Create(new CreateOrderRequest(
            StoreId: 1, MemberId: member.Id,
            Items: new[] { new OrderItemInputDto(svc.Id, tech.Id, AssignmentSource: "Designation") },
            Remark: null), default)).Result as ObjectResult;

        (result!.Value as OrderDto)!.Items.Single().AssignmentSource.Should().Be("Designation");
    }

    [Fact]
    public async Task Create_WithoutSource_DefaultsToDesignation()
    {
        // 老 CS 客户端 / 老 Swagger 不传 AssignmentSource 时，服务端按 Designation 兜底
        // 避免"沉默 = Rotation"把指定率虚高
        var (db, ctx, member, svc, tech) = Seed();
        var ctl = NewController(db, ctx);

        var result = (await ctl.Create(new CreateOrderRequest(
            StoreId: 1, MemberId: member.Id,
            Items: new[] { new OrderItemInputDto(svc.Id, tech.Id) },
            Remark: null), default)).Result as ObjectResult;

        (result!.Value as OrderDto)!.Items.Single().AssignmentSource.Should().Be("Designation");
    }

    [Fact]
    public async Task Create_WithInvalidSourceString_DefaultsToDesignation()
    {
        // 防 fuzz：任何无法 parse 的字符串都走 Designation 兜底，不抛
        var (db, ctx, member, svc, tech) = Seed();
        var ctl = NewController(db, ctx);

        var result = (await ctl.Create(new CreateOrderRequest(
            StoreId: 1, MemberId: member.Id,
            Items: new[] { new OrderItemInputDto(svc.Id, tech.Id, AssignmentSource: "garbage") },
            Remark: null), default)).Result as ObjectResult;

        (result!.Value as OrderDto)!.Items.Single().AssignmentSource.Should().Be("Designation");
    }

    [Fact]
    public async Task AddItems_PersistsAssignmentSource()
    {
        var (db, ctx, member, svc, tech) = Seed();
        var ctl = NewController(db, ctx);

        // 先建一单（Designation），再加一钟（Rotation）
        var created = (await ctl.Create(new CreateOrderRequest(
            StoreId: 1, MemberId: member.Id,
            Items: new[] { new OrderItemInputDto(svc.Id, tech.Id, AssignmentSource: "Designation") },
            Remark: null), default)).Result as ObjectResult;
        var orderId = (created!.Value as OrderDto)!.Id;

        var added = (await ctl.AddItems(orderId, new AddOrderItemsRequest(
            new[] { new OrderItemInputDto(svc.Id, tech.Id, AssignmentSource: "Rotation") }), default))
            .Result as OkObjectResult;
        var dto = added!.Value as OrderDto;
        dto!.Items.Should().HaveCount(2);
        dto.Items.Should().Contain(i => i.AssignmentSource == "Rotation" && i.IsAddOn);
        dto.Items.Should().Contain(i => i.AssignmentSource == "Designation" && !i.IsAddOn);
    }

    [Fact]
    public async Task TransferTechnician_PreservesAssignmentSource()
    {
        // 转钟换人不改 source：原来 Rotation 的项转给另一位技师后仍是 Rotation
        var (db, ctx, member, svc, tech) = Seed();
        var tech2 = new User
        {
            Id = 11, TenantId = 1, StoreId = 1, Username = "t2",
            PasswordHash = "x", Role = UserRole.Technician, IsActive = true, EmployeeNo = 2
        };
        db.Users.Add(tech2);
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var created = (await ctl.Create(new CreateOrderRequest(
            StoreId: 1, MemberId: member.Id,
            Items: new[] { new OrderItemInputDto(svc.Id, tech.Id, AssignmentSource: "Rotation") },
            Remark: null), default)).Result as ObjectResult;
        var dto = created!.Value as OrderDto;
        var itemId = dto!.Items.Single().Id;

        await ctl.TransferTechnician(dto.Id, itemId,
            new TransferTechnicianRequest(NewTechnicianId: tech2.Id, Reason: "试转"), default);

        var item = await db.OrderItems.AsNoTracking().FirstAsync(i => i.Id == itemId);
        item.TechnicianId.Should().Be(tech2.Id);
        item.AssignmentSource.Should().Be(AssignmentSource.Rotation, "转钟换人不改来源标记");
    }

    [Fact]
    public async Task Checkout_UsesSourceSpecificCommissionRule()
    {
        // 端到端：(svc+tech) 配两条规则，Rotation 30 元固定 / Designation 20%。
        // 会员单价 180 → Rotation 单 CommissionAmount = 30（固定）；
        // Designation 单 CommissionAmount = 180 × 20% = 36
        var (db, ctx, member, svc, tech) = Seed();
        db.CommissionRules.AddRange(
            new CommissionRule
            {
                TenantId = 1, ServiceId = svc.Id, TechnicianId = tech.Id,
                AssignmentSource = AssignmentSource.Rotation,
                RuleType = CommissionRuleType.FixedAmount, Amount = 30m, IsActive = true
            },
            new CommissionRule
            {
                TenantId = 1, ServiceId = svc.Id, TechnicianId = tech.Id,
                AssignmentSource = AssignmentSource.Designation,
                RuleType = CommissionRuleType.Percentage, Amount = 20m, IsActive = true
            });
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);

        // ---- Rotation 单 ----
        var createdR = (await ctl.Create(new CreateOrderRequest(
            StoreId: 1, MemberId: member.Id,
            Items: new[] { new OrderItemInputDto(svc.Id, tech.Id, AssignmentSource: "Rotation") },
            Remark: null), default)).Result as ObjectResult;
        var orderRId = (createdR!.Value as OrderDto)!.Id;
        var settledR = (await ctl.Checkout(orderRId, new CheckoutRequest(
            PayMethod: "Cash", PaidAmount: 200m, DiscountAmount: 0m), default))
            .Result as OkObjectResult;
        (settledR!.Value as OrderDto)!.Items.Single().CommissionAmount.Should().Be(30m);

        // ---- Designation 单 ----
        var createdD = (await ctl.Create(new CreateOrderRequest(
            StoreId: 1, MemberId: member.Id,
            Items: new[] { new OrderItemInputDto(svc.Id, tech.Id, AssignmentSource: "Designation") },
            Remark: null), default)).Result as ObjectResult;
        var orderDId = (createdD!.Value as OrderDto)!.Id;
        var settledD = (await ctl.Checkout(orderDId, new CheckoutRequest(
            PayMethod: "Cash", PaidAmount: 200m, DiscountAmount: 0m), default))
            .Result as OkObjectResult;
        (settledD!.Value as OrderDto)!.Items.Single().CommissionAmount.Should().Be(36m);
    }
}
