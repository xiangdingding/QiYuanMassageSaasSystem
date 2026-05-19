using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Storefront;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MassageSaas.UnitTests.Storefront;

public class StorefrontControllerTests
{
    private static (ApplicationDbContext Db, TenantContext Ctx) Seed()
    {
        var ctx = new TenantContext();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"storefront_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();

        db.Tenants.Add(new Tenant { Id = 1, Name = "测试店", ContactPhone = "13800000000", Status = TenantStatus.Active });
        db.Stores.Add(new Store { Id = 1, TenantId = 1, Name = "总店", IsActive = true });

        db.ServiceItems.Add(new ServiceItem
        {
            Id = 1, TenantId = 1, Code = "S1", Name = "全身按摩",
            DurationMinutes = 60, Price = 198m, MemberPrice = 158m, IsActive = true
        });
        db.ServiceItems.Add(new ServiceItem
        {
            Id = 2, TenantId = 1, Code = "S2", Name = "已停用项",
            DurationMinutes = 30, Price = 99m, MemberPrice = 99m, IsActive = false
        });

        db.Users.Add(new User
        {
            Id = 10, TenantId = 1, StoreId = 1, Username = "tech1", PasswordHash = "x",
            RealName = "李师傅", Role = UserRole.Technician, IsActive = true,
            TechnicianLevel = TechnicianLevel.Master, IsBlind = true, Specialties = "肩颈,足疗"
        });
        db.Users.Add(new User
        {
            Id = 11, TenantId = 1, StoreId = 1, Username = "tech2", PasswordHash = "x",
            RealName = "离职师傅", Role = UserRole.Technician, IsActive = false
        });
        db.Users.Add(new User
        {
            Id = 12, TenantId = 1, StoreId = 1, Username = "cashier", PasswordHash = "x",
            RealName = "收银小王", Role = UserRole.Cashier, IsActive = true
        });

        var member = new Member
        {
            Id = 100, TenantId = 1, StoreId = 1, CardNo = "VIP100", Phone = "13900001111",
            Name = "周女士", Balance = 520m, Level = MemberLevel.Gold, IsActive = true,
            WechatOpenId = "openid-bound"
        };
        db.Members.Add(member);
        db.MemberPackages.Add(new MemberPackage
        {
            Id = 200, TenantId = 1, MemberId = 100, StoreId = 1,
            Kind = MemberPackageKind.Counter, Title = "肩颈 10 次卡",
            TotalCount = 10, RemainCount = 7, Status = MemberPackageStatus.Active
        });
        db.MemberPackages.Add(new MemberPackage
        {
            Id = 201, TenantId = 1, MemberId = 100, StoreId = 1,
            Kind = MemberPackageKind.Counter, Title = "已用完卡",
            TotalCount = 5, RemainCount = 0, Status = MemberPackageStatus.Used
        });
        db.SaveChanges();
        return (db, ctx);
    }

    private static StorefrontController NewController(ApplicationDbContext db, TenantContext ctx) => new(db, ctx);

    [Fact]
    public async Task Services_returns_only_active_services()
    {
        var (db, ctx) = Seed();
        var result = await NewController(db, ctx).Services(storeId: 1, default);

        var list = (result.Result as OkObjectResult)!.Value as IReadOnlyList<StorefrontServiceDto>;
        list.Should().ContainSingle();
        list![0].Name.Should().Be("全身按摩");
        list[0].MemberPrice.Should().Be(158m);
    }

    [Fact]
    public async Task Services_rejects_unknown_store()
    {
        var (db, ctx) = Seed();
        var result = await NewController(db, ctx).Services(storeId: 999, default);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Technicians_returns_only_active_technicians()
    {
        var (db, ctx) = Seed();
        var result = await NewController(db, ctx).Technicians(storeId: 1, default);

        var list = (result.Result as OkObjectResult)!.Value as IReadOnlyList<StorefrontTechnicianDto>;
        list.Should().ContainSingle();
        list![0].Name.Should().Be("李师傅");
        list[0].IsBlind.Should().BeTrue();
        list[0].Level.Should().Be("Master");
    }

    [Fact]
    public async Task Member_returns_summary_with_active_packages()
    {
        var (db, ctx) = Seed();
        var result = await NewController(db, ctx).Member("openid-bound", default);

        var dto = (result.Result as OkObjectResult)!.Value as StorefrontMemberDto;
        dto!.CardNo.Should().Be("VIP100");
        dto.Balance.Should().Be(520m);
        dto.Packages.Should().ContainSingle("已用完的套餐不应返回");
        dto.Packages[0].RemainCount.Should().Be(7);
    }

    [Fact]
    public async Task Member_returns_NotFound_for_unbound_openId()
    {
        var (db, ctx) = Seed();
        var result = await NewController(db, ctx).Member("openid-unknown", default);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}
