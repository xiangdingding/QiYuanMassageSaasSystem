using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Members;
using MassageSaas.Shared.Payroll;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace MassageSaas.UnitTests.Members;

/// <summary>
/// 推荐返佣 / 提成端到端流程：配置(Tenant) → 开卡(顾客+员工推荐人) → 充值 → 工资单汇总。
/// 验证三条链路真正贯通，以及「顾客返佣方式二选一/全关」开关与「员工固定/百分比」两种模式。
/// </summary>
public class ReferralFlowTests
{
    private static (ApplicationDbContext Db, TenantContext Ctx, Tenant Tenant) NewDb()
    {
        var ctx = new TenantContext { TenantId = 1, UserId = 99 };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"db_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();
        var t = new Tenant { Id = 1, Name = "T", ContactPhone = "x" };
        db.Tenants.Add(t);
        db.Stores.Add(new Store { Id = 1, TenantId = 1, Name = "总店", IsActive = true });
        db.SaveChanges();
        return (db, ctx, t);
    }

    private static Member AddReferrer(ApplicationDbContext db, long id, string cardNo)
    {
        var m = new Member
        {
            Id = id, TenantId = 1, StoreId = 1, CardNo = cardNo, Phone = $"139{id:0000}",
            Name = $"荐{id}", Balance = 0m, Discount = 1m
        };
        db.Members.Add(m);
        db.SaveChanges();
        return m;
    }

    private static User AddStaff(ApplicationDbContext db, long id, string name)
    {
        var u = new User
        {
            Id = id, TenantId = 1, StoreId = 1, Username = $"u{id}", PasswordHash = "x",
            RealName = name, Role = UserRole.Technician, IsActive = true, EmployeeNo = (int)id
        };
        db.Users.Add(u);
        db.SaveChanges();
        return u;
    }

    private static MembersController NewMembers(ApplicationDbContext db, TenantContext ctx) =>
        new(db, ctx, NullLogger<MembersController>.Instance);

    private static PayrollController NewPayroll(ApplicationDbContext db, TenantContext ctx) =>
        new(db, ctx, NullLogger<PayrollController>.Instance);

    private static CreateMemberRequest OpenCard(string cardNo, decimal paid, long? byMember, long? byStaff) =>
        new(StoreId: 1, CardNo: cardNo, Phone: "13800000001", Name: "新客", Gender: null, Birthday: null,
            InitialBalance: paid, ReferredByMemberId: byMember, ReferredByStaffId: byStaff);

    /// <summary>顾客=固定推荐费、员工=开卡实收百分比；全链路到工资单 NetTotal。</summary>
    [Fact]
    public async Task FullFlow_CustomerFixed_AndStaffPercent_FlowsIntoPayroll()
    {
        var (db, ctx, tenant) = NewDb();
        tenant.CustomerReferralMode = CustomerReferralMode.FixedPerCard;
        tenant.CustomerReferralFixedReward = 50m;
        tenant.StaffReferralMode = StaffReferralMode.PercentOfOpenCard;
        tenant.StaffReferralPercent = 10m;
        await db.SaveChangesAsync();

        AddReferrer(db, 1, "REF");
        AddStaff(db, 10, "技师A");

        var members = NewMembers(db, ctx);
        var created = await members.Create(OpenCard("NEW", 1000m, byMember: 1, byStaff: 10), default);
        (created.Result as ObjectResult)?.StatusCode.Should().BeOneOf(200, 201);

        // 顾客：固定推荐费 50；固定模式下开卡这次充值不再叠加百分比
        var referrer = await db.Members.FirstAsync(x => x.Id == 1);
        referrer.Balance.Should().Be(50m);
        referrer.ReferralRewardEarned.Should().Be(50m);

        // 员工：开卡提成 = 1000 × 10% = 100，落一条 StaffReferralRecord
        var staffRec = await db.StaffReferralRecords.SingleAsync();
        staffRec.StaffUserId.Should().Be(10);
        staffRec.Amount.Should().Be(100m);

        // 工资单：员工推荐提成汇总进 ReferralCommissionTotal 并计入 NetTotal
        var payroll = NewPayroll(db, ctx);
        var cn = DateTime.UtcNow.AddHours(8); // 业务月按北京时间归属
        var gen = await payroll.Generate(new GeneratePayrollRequest(1, cn.Year, cn.Month, null), default);
        var detail = (gen.Result as OkObjectResult)?.Value as PayrollPeriodDetailDto;
        detail.Should().NotBeNull();
        var item = detail!.Items.Single(i => i.UserId == 10);
        item.ReferralCommissionTotal.Should().Be(100m);
        item.NetTotal.Should().Be(100m, "base 0 + 提成 0 + 引荐提成 100 + 满勤 0");
    }

    /// <summary>顾客=充值返佣百分比：开卡这次返佣 + 后续每次充值都返佣。</summary>
    [Fact]
    public async Task CustomerPercent_GrantsOnOpenCardAndEachRecharge()
    {
        var (db, ctx, tenant) = NewDb();
        tenant.CustomerReferralMode = CustomerReferralMode.PercentPerRecharge;
        tenant.ReferralRewardPercent = 5m;
        await db.SaveChangesAsync();

        AddReferrer(db, 1, "REF");
        var members = NewMembers(db, ctx);

        // 开卡：被推荐人充值 1000 → 推荐人 +50
        var created = await members.Create(OpenCard("NEW", 1000m, byMember: 1, byStaff: null), default);
        var newId = (await db.Members.FirstAsync(x => x.CardNo == "NEW")).Id;
        (created.Result as ObjectResult)?.StatusCode.Should().BeOneOf(200, 201);
        (await db.Members.FirstAsync(x => x.Id == 1)).Balance.Should().Be(50m);

        // 再充值 2000 → 推荐人再 +100，累计 150
        await members.Recharge(new RechargeRequest(newId, 2000m, 0m, "Cash", null), default);
        (await db.Members.FirstAsync(x => x.Id == 1)).Balance.Should().Be(150m);

        // 未配员工推荐 → 无 StaffReferralRecord
        (await db.StaffReferralRecords.CountAsync()).Should().Be(0);
    }

    /// <summary>开关：顾客返佣方式=关闭，员工=固定金额；顾客不返佣、员工照记。</summary>
    [Fact]
    public async Task CustomerNone_StaffFixed_OnlyStaffEarns()
    {
        var (db, ctx, tenant) = NewDb();
        tenant.CustomerReferralMode = CustomerReferralMode.None;
        tenant.ReferralRewardPercent = 5m;          // 即便有值，关闭后也不返
        tenant.CustomerReferralFixedReward = 50m;   // 即便有值，关闭后也不返
        tenant.StaffReferralMode = StaffReferralMode.FixedPerCard;
        tenant.StaffReferralFixedAmount = 30m;
        await db.SaveChangesAsync();

        AddReferrer(db, 1, "REF");
        AddStaff(db, 10, "技师A");
        var members = NewMembers(db, ctx);

        await members.Create(OpenCard("NEW", 1000m, byMember: 1, byStaff: 10), default);

        // 顾客一分不返
        var referrer = await db.Members.FirstAsync(x => x.Id == 1);
        referrer.Balance.Should().Be(0m);
        referrer.ReferralRewardEarned.Should().Be(0m);
        (await db.MemberRechargeRecords.CountAsync(r => r.Kind == MemberRechargeKind.ReferralBonus)).Should().Be(0);

        // 员工固定提成 30
        (await db.StaffReferralRecords.SingleAsync()).Amount.Should().Be(30m);
    }
}
