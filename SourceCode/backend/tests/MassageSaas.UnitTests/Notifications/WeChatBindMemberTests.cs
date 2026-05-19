using FluentAssertions;
using MassageSaas.Api.Controllers;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Multitenancy;
using MassageSaas.Infrastructure.Notifications;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.WeChat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MassageSaas.UnitTests.Notifications;

public class WeChatBindMemberTests
{
    private sealed class StubMiniProgramClient : IWeChatMiniProgramClient
    {
        private readonly WeChatSession? _session;
        public StubMiniProgramClient(WeChatSession? session) => _session = session;
        public bool IsConfigured => true;
        public Task<WeChatSession?> Code2SessionAsync(string code, CancellationToken ct) => Task.FromResult(_session);
    }

    private static (ApplicationDbContext Db, TenantContext Ctx) Seed(out Store store)
    {
        var ctx = new TenantContext();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"wechat_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options, ctx);
        ctx.BypassTenantFilter();
        db.Tenants.Add(new Tenant { Id = 1, Name = "测试店", ContactPhone = "13800000000", Status = TenantStatus.Active });
        store = new Store { Id = 1, TenantId = 1, Name = "总店", IsActive = true };
        db.Stores.Add(store);
        db.Members.Add(new Member
        {
            Id = 1, TenantId = 1, StoreId = 1,
            CardNo = "VIP001", Phone = "13900001111", Name = "王女士",
            Balance = 388m, Level = MemberLevel.Gold, IsActive = true
        });
        db.SaveChanges();
        return (db, ctx);
    }

    private static WeChatController NewController(
        ApplicationDbContext db, TenantContext ctx, WeChatOptions? options = null) =>
        new(db, ctx, new StubMiniProgramClient(new WeChatSession("OPENID-X", null)),
            Options.Create(options ?? new WeChatOptions()),
            NullLogger<WeChatController>.Instance);

    [Fact]
    public async Task BindMember_succeeds_and_persists_openId()
    {
        var (db, ctx) = Seed(out var store);
        var c = NewController(db, ctx);

        var result = await c.BindMember(new BindMemberRequest(store.Id, "13900001111", "OPENID-X"), default);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var dto = ok!.Value as BoundMemberDto;
        dto!.CardNo.Should().Be("VIP001");
        dto.Level.Should().Be("Gold");

        var saved = await db.Members.FindAsync(1L);
        saved!.WechatOpenId.Should().Be("OPENID-X");
    }

    [Fact]
    public async Task BindMember_returns_NotFound_for_unknown_phone()
    {
        var (db, ctx) = Seed(out var store);
        var c = NewController(db, ctx);

        var result = await c.BindMember(new BindMemberRequest(store.Id, "13700000000", "OPENID-X"), default);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task BindMember_rejects_unknown_store()
    {
        var (db, ctx) = Seed(out _);
        var c = NewController(db, ctx);

        var result = await c.BindMember(new BindMemberRequest(999, "13900001111", "OPENID-X"), default);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BindMember_is_idempotent_on_repeat()
    {
        var (db, ctx) = Seed(out var store);
        var c = NewController(db, ctx);

        await c.BindMember(new BindMemberRequest(store.Id, "13900001111", "OPENID-X"), default);
        var result = await c.BindMember(new BindMemberRequest(store.Id, "13900001111", "OPENID-X"), default);

        result.Result.Should().BeOfType<OkObjectResult>();
        (await db.Members.FindAsync(1L))!.WechatOpenId.Should().Be("OPENID-X");
    }

    [Fact]
    public async Task Session_returns_openId()
    {
        var (db, ctx) = Seed(out _);
        var c = NewController(db, ctx);

        var result = await c.Session(new WeChatSessionRequest("login-code"), default);
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ((WeChatSessionResponse)ok!.Value!).OpenId.Should().Be("OPENID-X");
    }

    [Fact]
    public void SubscribeTemplates_returns_only_configured_customer_kinds()
    {
        var (db, ctx) = Seed(out _);
        var options = new WeChatOptions();
        options.Templates["AppointmentReminder"] = "TMPL_APT";
        options.Templates["RechargeArrived"] = "TMPL_RECHARGE";
        options.Templates["MemberBirthday"] = "";                 // 未真正配置，应被跳过
        options.Templates["ServiceComplaintAlert"] = "TMPL_CMPL"; // 给店长的，不暴露给顾客
        var c = NewController(db, ctx, options);

        var ok = c.SubscribeTemplates().Result as OkObjectResult;
        ok.Should().NotBeNull();
        var map = ((SubscribeTemplatesResponse)ok!.Value!).Templates;

        map.Should().ContainKey("AppointmentReminder").And.ContainKey("RechargeArrived");
        map.Should().NotContainKey("MemberBirthday");
        map.Should().NotContainKey("ServiceComplaintAlert");
        map["AppointmentReminder"].Should().Be("TMPL_APT");
    }

    [Fact]
    public void SubscribeTemplates_returns_empty_when_nothing_configured()
    {
        var (db, ctx) = Seed(out _);
        var c = NewController(db, ctx);

        var ok = c.SubscribeTemplates().Result as OkObjectResult;
        ((SubscribeTemplatesResponse)ok!.Value!).Templates.Should().BeEmpty();
    }
}
