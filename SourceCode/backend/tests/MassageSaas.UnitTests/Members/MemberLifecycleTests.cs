using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Members;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace MassageSaas.UnitTests.Members;

public class MemberLifecycleTests
{
    private static (ApplicationDbContext Db, TenantContext Ctx, Tenant Tenant, Store Store) NewDb(decimal referralPct = 0m)
    {
        var ctx = new TenantContext { TenantId = 1, UserId = 99 };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"db_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();
        var t = new Tenant
        {
            Id = 1, Name = "T", ContactPhone = "x", ReferralRewardPercent = referralPct,
            CustomerReferralMode = referralPct > 0m ? CustomerReferralMode.PercentPerRecharge : CustomerReferralMode.None
        };
        db.Tenants.Add(t);
        var s = new Store { Id = 1, TenantId = 1, Name = "总店", IsActive = true };
        db.Stores.Add(s);
        db.SaveChanges();
        return (db, ctx, t, s);
    }

    private static Member AddMember(ApplicationDbContext db, long id, string cardNo,
        decimal balance = 0m, decimal totalRecharge = 0m, long? referredBy = null)
    {
        var m = new Member
        {
            Id = id, TenantId = 1, StoreId = 1, CardNo = cardNo, Phone = $"138{id:0000}",
            Name = $"会员{id}",
            Balance = balance, TotalRecharge = totalRecharge, Discount = 1m,
            ReferredByMemberId = referredBy
        };
        db.Members.Add(m);
        db.SaveChanges();
        return m;
    }

    private static MembersController NewController(ApplicationDbContext db, TenantContext ctx) =>
        new(db, ctx, NullLogger<MembersController>.Instance);

    // ---------- Refund ----------

    [Fact]
    public async Task Refund_WithinBalance_DeductsAndClosesCard()
    {
        var (db, ctx, _, _) = NewDb();
        AddMember(db, 100, "C100", balance: 300m, totalRecharge: 300m);
        var ctl = NewController(db, ctx);

        var resp = (await ctl.Refund(100, new RefundMemberRequest(200m, "Cash", "客户要求"), default)).Result as OkObjectResult;
        var rec = resp!.Value as RechargeRecordDto;

        rec!.Amount.Should().Be(200m);
        rec.Kind.Should().Be("Refund");
        rec.BalanceAfter.Should().Be(100m);

        var m = await db.Members.FirstAsync(x => x.Id == 100);
        m.Balance.Should().Be(100m);
        m.IsActive.Should().BeFalse();
        m.ClosedAt.Should().NotBeNull();
        m.CloseReason.Should().Be("客户要求");
    }

    [Fact]
    public async Task Refund_ExceedingBalance_ReturnsBadRequest()
    {
        var (db, ctx, _, _) = NewDb();
        AddMember(db, 100, "C100", balance: 50m);
        var ctl = NewController(db, ctx);
        var resp = (await ctl.Refund(100, new RefundMemberRequest(80m, "Cash", null), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Refund_OnAlreadyClosedCard_Rejects()
    {
        var (db, ctx, _, _) = NewDb();
        var m = AddMember(db, 100, "C100", balance: 50m);
        m.IsActive = false; m.ClosedAt = DateTime.UtcNow; m.CloseReason = "x";
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var resp = (await ctl.Refund(100, new RefundMemberRequest(10m, "Cash", null), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Refund_ToMemberCard_Rejects()
    {
        var (db, ctx, _, _) = NewDb();
        AddMember(db, 100, "C100", balance: 50m);
        var ctl = NewController(db, ctx);
        var resp = (await ctl.Refund(100, new RefundMemberRequest(10m, "MemberCard", null), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(400);
    }

    // ---------- Transfer ----------

    [Fact]
    public async Task Transfer_ToExistingMember_MovesBalanceAndClosesSource()
    {
        var (db, ctx, _, _) = NewDb();
        AddMember(db, 100, "C100", balance: 240m, totalRecharge: 240m);
        AddMember(db, 200, "C200", balance: 60m, totalRecharge: 60m);

        var ctl = NewController(db, ctx);
        var resp = (await ctl.Transfer(100, new TransferMemberRequest(200, null, null, null, "送朋友"), default)).Result as OkObjectResult;
        var dto = resp!.Value as MemberDto;

        dto!.Id.Should().Be(200);
        dto.Balance.Should().Be(300m, "60 + 240");
        dto.TotalRecharge.Should().Be(60m, "转赠不计入 TotalRecharge，避免被刷成高等级");

        var src = await db.Members.FirstAsync(x => x.Id == 100);
        src.Balance.Should().Be(0);
        src.IsActive.Should().BeFalse();
        src.CloseReason.Should().Contain("C200");

        var records = await db.MemberRechargeRecords.OrderBy(r => r.Id).ToListAsync();
        records.Should().Contain(r => r.MemberId == 100 && r.Kind == MemberRechargeKind.TransferOut && r.CounterpartyMemberId == 200);
        records.Should().Contain(r => r.MemberId == 200 && r.Kind == MemberRechargeKind.TransferIn && r.CounterpartyMemberId == 100);
    }

    [Fact]
    public async Task Transfer_ToSelf_Rejects()
    {
        var (db, ctx, _, _) = NewDb();
        AddMember(db, 100, "C100", balance: 100m);
        var ctl = NewController(db, ctx);
        var resp = (await ctl.Transfer(100, new TransferMemberRequest(100, null, null, null, null), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Transfer_ZeroBalance_Rejects()
    {
        var (db, ctx, _, _) = NewDb();
        AddMember(db, 100, "C100", balance: 0m);
        AddMember(db, 200, "C200");
        var ctl = NewController(db, ctx);
        var resp = (await ctl.Transfer(100, new TransferMemberRequest(200, null, null, null, null), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Transfer_CreatesNewMember_WhenNewMemberFieldsProvided()
    {
        var (db, ctx, _, _) = NewDb();
        AddMember(db, 100, "C100", balance: 150m);
        var ctl = NewController(db, ctx);
        var resp = (await ctl.Transfer(100,
            new TransferMemberRequest(null, "C999", "13912345678", "新人", null), default)).Result as OkObjectResult;
        var dto = resp!.Value as MemberDto;

        dto!.CardNo.Should().Be("C999");
        dto.Balance.Should().Be(150m);
        dto.IsActive.Should().BeTrue();
    }

    // ---------- Referral bonus ----------

    [Fact]
    public async Task Recharge_GrantsReferralBonus_WhenTenantHasPercent()
    {
        var (db, ctx, _, _) = NewDb(referralPct: 5m);
        AddMember(db, 1, "REF", balance: 0m);
        AddMember(db, 2, "NEW", referredBy: 1);

        var ctl = NewController(db, ctx);
        await ctl.Recharge(new RechargeRequest(2, 1000m, 0m, "Cash", null), default);

        var referrer = await db.Members.FirstAsync(x => x.Id == 1);
        referrer.Balance.Should().Be(50m, "1000 × 5%");
        referrer.ReferralRewardEarned.Should().Be(50m);

        var bonus = await db.MemberRechargeRecords.FirstAsync(r => r.MemberId == 1 && r.Kind == MemberRechargeKind.ReferralBonus);
        bonus.Amount.Should().Be(50m);
        bonus.CounterpartyMemberId.Should().Be(2);
    }

    [Fact]
    public async Task Recharge_NoReferralBonus_WhenPercentIsZero()
    {
        var (db, ctx, _, _) = NewDb(referralPct: 0m);
        AddMember(db, 1, "REF");
        AddMember(db, 2, "NEW", referredBy: 1);

        var ctl = NewController(db, ctx);
        await ctl.Recharge(new RechargeRequest(2, 1000m, 0m, "Cash", null), default);

        var referrer = await db.Members.FirstAsync(x => x.Id == 1);
        referrer.Balance.Should().Be(0);
        referrer.ReferralRewardEarned.Should().Be(0);
    }

    [Fact]
    public async Task Recharge_NoPercentBonus_WhenModeIsFixedPerCard()
    {
        // 即使配了百分比，但返佣方式选了"固定推荐费"，充值时不走百分比返佣（二选一开关）。
        var (db, ctx, tenant, _) = NewDb(referralPct: 5m);
        tenant.CustomerReferralMode = CustomerReferralMode.FixedPerCard;
        await db.SaveChangesAsync();
        AddMember(db, 1, "REF");
        AddMember(db, 2, "NEW", referredBy: 1);

        var ctl = NewController(db, ctx);
        await ctl.Recharge(new RechargeRequest(2, 1000m, 0m, "Cash", null), default);

        var referrer = await db.Members.FirstAsync(x => x.Id == 1);
        referrer.Balance.Should().Be(0m);
        referrer.ReferralRewardEarned.Should().Be(0m);
    }

    [Fact]
    public async Task Recharge_NoPercentBonus_WhenModeIsNone()
    {
        // 返佣方式关闭：即便百分比有值也不返佣。
        var (db, ctx, tenant, _) = NewDb(referralPct: 5m);
        tenant.CustomerReferralMode = CustomerReferralMode.None;
        await db.SaveChangesAsync();
        AddMember(db, 1, "REF");
        AddMember(db, 2, "NEW", referredBy: 1);

        var ctl = NewController(db, ctx);
        await ctl.Recharge(new RechargeRequest(2, 1000m, 0m, "Cash", null), default);

        var referrer = await db.Members.FirstAsync(x => x.Id == 1);
        referrer.Balance.Should().Be(0m);
    }

    [Fact]
    public async Task Recharge_NoBonusIfNoReferrer()
    {
        var (db, ctx, _, _) = NewDb(referralPct: 5m);
        AddMember(db, 2, "NEW");
        var ctl = NewController(db, ctx);
        await ctl.Recharge(new RechargeRequest(2, 1000m, 0m, "Cash", null), default);

        var bonusRows = await db.MemberRechargeRecords.Where(r => r.Kind == MemberRechargeKind.ReferralBonus).CountAsync();
        bonusRows.Should().Be(0);
    }

    [Fact]
    public async Task Recharge_NoBonusIfReferrerClosed()
    {
        var (db, ctx, _, _) = NewDb(referralPct: 5m);
        var referrer = AddMember(db, 1, "REF");
        referrer.IsActive = false; referrer.ClosedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        AddMember(db, 2, "NEW", referredBy: 1);

        var ctl = NewController(db, ctx);
        await ctl.Recharge(new RechargeRequest(2, 1000m, 0m, "Cash", null), default);

        var bonusRows = await db.MemberRechargeRecords.Where(r => r.Kind == MemberRechargeKind.ReferralBonus).CountAsync();
        bonusRows.Should().Be(0);
    }

    [Fact]
    public async Task Referrals_ReturnsReferredList()
    {
        var (db, ctx, _, _) = NewDb();
        var referrer = AddMember(db, 1, "REF");
        referrer.ReferralRewardEarned = 88m;
        AddMember(db, 2, "A", referredBy: 1, totalRecharge: 1000m);
        AddMember(db, 3, "B", referredBy: 1, totalRecharge: 500m);
        AddMember(db, 4, "OTHER");
        await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var resp = (await ctl.Referrals(1, default)).Result as OkObjectResult;
        var summary = resp!.Value as ReferralSummaryDto;

        summary!.ReferrerMemberId.Should().Be(1);
        summary.TotalRewardEarned.Should().Be(88m);
        summary.ReferredCount.Should().Be(2);
        summary.ReferredMembers.Select(x => x.MemberId).Should().BeEquivalentTo(new[] { 2L, 3L });
    }

    // ---------- Recharge on closed card ----------

    [Fact]
    public async Task Recharge_ToClosedMember_Rejects()
    {
        var (db, ctx, _, _) = NewDb();
        var m = AddMember(db, 100, "C100");
        m.IsActive = false; await db.SaveChangesAsync();

        var ctl = NewController(db, ctx);
        var resp = (await ctl.Recharge(new RechargeRequest(100, 100m, 0m, "Cash", null), default)).Result as ObjectResult;
        resp!.StatusCode.Should().Be(409);
    }
}
