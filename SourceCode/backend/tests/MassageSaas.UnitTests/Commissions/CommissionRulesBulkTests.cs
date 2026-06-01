using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Commissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MassageSaas.UnitTests.Commissions;

public class CommissionRulesBulkTests
{
    private static (ApplicationDbContext Db, CommissionRulesController Ctrl) Seed()
    {
        var ctx = new TenantContext();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"bulkc_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();
        db.Tenants.Add(new Tenant { Id = 1, Name = "T", ContactPhone = "1", Status = TenantStatus.Active });
        db.Stores.Add(new Store { Id = 1, TenantId = 1, Name = "S", IsActive = true });
        db.ServiceItems.AddRange(
            new ServiceItem { Id = 100, TenantId = 1, Code = "A", Name = "肩颈", Price = 100m, MemberPrice = 80m, DurationMinutes = 30 },
            new ServiceItem { Id = 101, TenantId = 1, Code = "B", Name = "全身", Price = 200m, MemberPrice = 160m, DurationMinutes = 60 });
        db.Users.AddRange(
            new User { Id = 200, TenantId = 1, StoreId = 1, Username = "tech1", PasswordHash = "x", Role = UserRole.Technician, IsActive = true },
            new User { Id = 201, TenantId = 1, StoreId = 1, Username = "tech2", PasswordHash = "x", Role = UserRole.Technician, IsActive = true },
            new User { Id = 999, TenantId = 1, StoreId = 1, Username = "cashier", PasswordHash = "x", Role = UserRole.Cashier, IsActive = true });
        db.SaveChanges();
        return (db, new CommissionRulesController(db));
    }

    private static BulkCommissionRuleRequest Template(long[] sids, long[] tids, bool overwrite,
        decimal? rotation = null, decimal? designation = null) =>
        new(
            ServiceIds: sids, TechnicianIds: tids,
            RuleType: "FixedAmount", Amount: 20m, TieredRulesJson: null,
            Priority: 0, IsActive: true,
            RotationAmount: rotation, DesignationAmount: designation,
            OverwriteExisting: overwrite);

    [Fact]
    public async Task Bulk_CreatesCartesianProduct()
    {
        var (db, c) = Seed();
        var res = await c.Bulk(Template(new[] { 100L, 101L }, new[] { 200L, 201L }, overwrite: false,
            rotation: 30m, designation: 40m), default);
        var ok = res.Result as OkObjectResult;
        var data = ok!.Value as BulkCommissionRuleResult;
        data!.Created.Should().Be(4);
        data.Updated.Should().Be(0);
        data.Skipped.Should().Be(0);
        (await db.CommissionRules.CountAsync()).Should().Be(4);
    }

    [Fact]
    public async Task Bulk_SkipsExistingWhenOverwriteFalse()
    {
        var (db, c) = Seed();
        db.CommissionRules.Add(new CommissionRule
        {
            TenantId = 1, ServiceId = 100, TechnicianId = 200,
            RuleType = CommissionRuleType.FixedAmount, Amount = 99m, IsActive = true
        });
        await db.SaveChangesAsync();

        var res = await c.Bulk(Template(new[] { 100L, 101L }, new[] { 200L, 201L }, overwrite: false), default);
        var data = (res.Result as OkObjectResult)!.Value as BulkCommissionRuleResult;
        data!.Created.Should().Be(3);
        data.Skipped.Should().Be(1);
        data.Updated.Should().Be(0);
        // 老规则金额未被改动
        var old = await db.CommissionRules.FirstAsync(r => r.ServiceId == 100 && r.TechnicianId == 200);
        old.Amount.Should().Be(99m);
    }

    [Fact]
    public async Task Bulk_OverwritesExistingWhenOverwriteTrue()
    {
        var (db, c) = Seed();
        db.CommissionRules.Add(new CommissionRule
        {
            TenantId = 1, ServiceId = 100, TechnicianId = 200,
            RuleType = CommissionRuleType.FixedAmount, Amount = 99m, IsActive = true
        });
        await db.SaveChangesAsync();

        var res = await c.Bulk(Template(new[] { 100L, 101L }, new[] { 200L, 201L }, overwrite: true,
            rotation: 30m, designation: 40m), default);
        var data = (res.Result as OkObjectResult)!.Value as BulkCommissionRuleResult;
        data!.Created.Should().Be(3);
        data.Updated.Should().Be(1);
        data.Skipped.Should().Be(0);
        var updated = await db.CommissionRules.FirstAsync(r => r.ServiceId == 100 && r.TechnicianId == 200);
        updated.Amount.Should().Be(20m);
        updated.RotationAmount.Should().Be(30m);
        updated.DesignationAmount.Should().Be(40m);
    }

    [Fact]
    public async Task Bulk_DoesNotTouchSourceSpecificRules()
    {
        var (db, c) = Seed();
        // 一条 Rotation 专属规则（AssignmentSource != null）应该不被批量更新影响
        db.CommissionRules.Add(new CommissionRule
        {
            TenantId = 1, ServiceId = 100, TechnicianId = 200,
            AssignmentSource = AssignmentSource.Rotation,
            RuleType = CommissionRuleType.FixedAmount, Amount = 55m, IsActive = true
        });
        await db.SaveChangesAsync();

        var res = await c.Bulk(Template(new[] { 100L }, new[] { 200L }, overwrite: true), default);
        var data = (res.Result as OkObjectResult)!.Value as BulkCommissionRuleResult;
        data!.Created.Should().Be(1, "新建的是 AssignmentSource=null 的通用规则，与 Rotation 专属规则共存");
        data.Updated.Should().Be(0);

        var sourceRule = await db.CommissionRules.FirstAsync(r => r.AssignmentSource == AssignmentSource.Rotation);
        sourceRule.Amount.Should().Be(55m, "Rotation 专属老规则不应被批量改动");
    }

    [Fact]
    public async Task Bulk_RejectsEmptyServiceList()
    {
        var (_, c) = Seed();
        var res = await c.Bulk(Template(Array.Empty<long>(), new[] { 200L }, overwrite: false), default);
        res.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Bulk_RejectsNonTechnicianUserIds()
    {
        var (db, c) = Seed();
        // 999 是收银员，不应被算作技师
        var res = await c.Bulk(Template(new[] { 100L }, new[] { 999L }, overwrite: false), default);
        res.Result.Should().BeOfType<BadRequestObjectResult>("收银员不是技师，没有合法组合");
        (await db.CommissionRules.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Bulk_RejectsDualAmountOnTieredType()
    {
        var (_, c) = Seed();
        var bad = new BulkCommissionRuleRequest(
            ServiceIds: new[] { 100L }, TechnicianIds: new[] { 200L },
            RuleType: "Tiered", Amount: 20m, TieredRulesJson: "[]",
            Priority: 0, IsActive: true,
            RotationAmount: 10m, DesignationAmount: 15m,
            OverwriteExisting: false);
        var res = await c.Bulk(bad, default);
        res.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
