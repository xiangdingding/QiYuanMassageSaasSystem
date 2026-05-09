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

public class OrderCheckoutTests
{
    private static (ApplicationDbContext Db, TenantContext Ctx, Member Member, ServiceItem Service, User Tech, Store Store)
        Seed(decimal balance = 500m, decimal discount = 1.0m, decimal price = 200m, decimal memberPrice = 180m)
    {
        var ctx = new TenantContext { TenantId = 1, UserId = 99 };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"db_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();

        var store = new Store { Id = 1, TenantId = 1, Name = "总店", IsActive = true };
        db.Stores.Add(store);

        var tech = new User
        {
            Id = 10, TenantId = 1, StoreId = 1, Username = "t1",
            PasswordHash = "x", Role = UserRole.Technician, IsActive = true, EmployeeNo = 1
        };
        db.Users.Add(tech);

        var svc = new ServiceItem
        {
            Id = 100, TenantId = 1, Code = "S1", Name = "60 分钟肩颈",
            DurationMinutes = 60, Price = price, MemberPrice = memberPrice, IsActive = true
        };
        db.ServiceItems.Add(svc);

        var member = new Member
        {
            Id = 1000, TenantId = 1, StoreId = 1, CardNo = "C001",
            Phone = "13800138000", Balance = balance, Discount = discount,
            TotalRecharge = balance, TotalConsumed = 0
        };
        db.Members.Add(member);

        db.SaveChanges();
        return (db, ctx, member, svc, tech, store);
    }

    private static OrdersController NewController(ApplicationDbContext db, TenantContext ctx) =>
        new(db, ctx, NullLogger<OrdersController>.Instance);

    [Fact]
    public async Task Checkout_WithMemberCard_DeductsBalanceAndComputesCommission()
    {
        var (db, ctx, member, svc, tech, _) = Seed(balance: 500m);

        db.CommissionRules.Add(new CommissionRule
        {
            TenantId = 1,
            ServiceId = null,
            TechnicianId = null,
            RuleType = CommissionRuleType.Percentage,
            Amount = 30m,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var created = (await ctl.Create(new CreateOrderRequest(
            StoreId: 1, MemberId: member.Id,
            Items: new[] { new OrderItemInputDto(svc.Id, tech.Id, Quantity: 1) },
            Remark: null), default)).Result as ObjectResult;
        var orderDto = created!.Value as OrderDto;
        orderDto!.Total.Should().Be(180m, "member price applies");

        var result = (await ctl.Checkout(orderDto.Id, new CheckoutRequest(
            PayMethod: "MemberCard", PaidAmount: 180m, DiscountAmount: 0m, Remark: null), default))
            .Result as OkObjectResult;
        var checkedOut = result!.Value as OrderDto;

        checkedOut!.Status.Should().Be("Completed");
        checkedOut.PaidAmount.Should().Be(180m);
        checkedOut.PayMethod.Should().Be("MemberCard");
        checkedOut.Items.Should().ContainSingle();
        checkedOut.Items[0].CommissionAmount.Should().Be(54m, "30% of 180");

        var memberAfter = await db.Members.AsNoTracking().FirstAsync(m => m.Id == member.Id);
        memberAfter.Balance.Should().Be(320m, "500 - 180");
        memberAfter.TotalConsumed.Should().Be(180m);
    }

    [Fact]
    public async Task Checkout_WithMemberCard_RejectsWhenBalanceInsufficient()
    {
        var (db, ctx, member, svc, tech, _) = Seed(balance: 50m);
        var ctl = NewController(db, ctx);

        var created = (await ctl.Create(new CreateOrderRequest(
            StoreId: 1, MemberId: member.Id,
            Items: new[] { new OrderItemInputDto(svc.Id, tech.Id) }, Remark: null), default))
            .Result as ObjectResult;
        var orderDto = created!.Value as OrderDto;

        var result = (await ctl.Checkout(orderDto!.Id,
            new CheckoutRequest(PayMethod: "MemberCard", PaidAmount: 180m), default))
            .Result as ObjectResult;

        result!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Refund_OfMemberCardOrder_RestoresBalance()
    {
        var (db, ctx, member, svc, tech, _) = Seed(balance: 500m);
        var ctl = NewController(db, ctx);

        var created = (await ctl.Create(new CreateOrderRequest(
            StoreId: 1, MemberId: member.Id,
            Items: new[] { new OrderItemInputDto(svc.Id, tech.Id) }, Remark: null), default))
            .Result as ObjectResult;
        var orderDto = created!.Value as OrderDto;
        await ctl.Checkout(orderDto!.Id, new CheckoutRequest(PayMethod: "MemberCard", PaidAmount: 180m), default);

        var refunded = (await ctl.Refund(orderDto.Id, new RefundRequest(Reason: "顾客取消"), default))
            .Result as OkObjectResult;
        var refundDto = refunded!.Value as OrderDto;

        refundDto!.Status.Should().Be("Refunded");
        var memberAfter = await db.Members.AsNoTracking().FirstAsync(m => m.Id == member.Id);
        memberAfter.Balance.Should().Be(500m);
        memberAfter.TotalConsumed.Should().Be(0m);
    }

    [Fact]
    public async Task Checkout_AppliesMemberDiscountAutomatically()
    {
        var (db, ctx, member, svc, tech, _) = Seed(balance: 500m, discount: 0.9m, price: 200m, memberPrice: 200m);
        var ctl = NewController(db, ctx);

        var created = (await ctl.Create(new CreateOrderRequest(
            StoreId: 1, MemberId: member.Id,
            Items: new[] { new OrderItemInputDto(svc.Id, tech.Id) }, Remark: null), default))
            .Result as ObjectResult;
        var orderDto = created!.Value as OrderDto;
        orderDto!.Total.Should().Be(200m);

        var result = (await ctl.Checkout(orderDto.Id, new CheckoutRequest(
            PayMethod: "Cash", PaidAmount: 180m, DiscountAmount: 0m), default))
            .Result as OkObjectResult;
        var dto = result!.Value as OrderDto;

        dto!.DiscountAmount.Should().Be(20m, "10% off via member.Discount=0.9");
        dto.PaidAmount.Should().Be(180m);
    }
}
