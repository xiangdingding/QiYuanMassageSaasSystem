using FluentAssertions;
using MassageSaas.Shared.Auth;

namespace MassageSaas.UnitTests.Auth;

/// <summary>
/// 店端菜单可见性回归测试。
/// 重点守护"切换账号后菜单仍停留在上一个登录人权限"的缺陷：
/// 菜单必须是【当前角色】的纯函数——同一份输入只取决于角色，不携带任何上次的状态。
/// </summary>
public class ShopMenuTests
{
    [Fact]
    public void Technician_sees_only_queue()
    {
        ShopMenu.VisibleKeys(ShopMenu.Technician).Should().Equal("queue");
    }

    [Fact]
    public void Owner_sees_full_menu_including_owner_only_pages()
    {
        var keys = ShopMenu.VisibleKeys(ShopMenu.Owner);
        keys.Should().Contain(new[] { "dashboard", "pos", "members", "services", "staff", "stores", "subscription", "queue" });
        // 店主能看到全部 21 项
        keys.Should().HaveCount(21);
    }

    [Fact]
    public void Cashier_has_pos_but_not_leadership_or_owner_pages()
    {
        var keys = ShopMenu.VisibleKeys(ShopMenu.Cashier);
        keys.Should().Contain("pos");
        keys.Should().Contain("queue");
        keys.Should().NotContain("dashboard");      // 经营看板，店长及以上
        keys.Should().NotContain("services");      // 店长及以上
        keys.Should().NotContain("staff");          // 店长及以上
        keys.Should().NotContain("stores");         // 仅店主
        keys.Should().NotContain("subscription");   // 仅店主
    }

    [Fact]
    public void Manager_has_leadership_pages_but_not_owner_only()
    {
        var keys = ShopMenu.VisibleKeys(ShopMenu.Manager);
        keys.Should().Contain(new[] { "dashboard", "pos", "services", "staff", "payroll" });
        keys.Should().NotContain("stores");
        keys.Should().NotContain("subscription");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("PlatformAdmin")] // 平台管理员不是店端角色，店端无任何菜单
    public void Unknown_or_empty_role_sees_nothing(string? role)
    {
        ShopMenu.VisibleKeys(role).Should().BeEmpty();
    }

    [Fact]
    public void Switching_account_recomputes_menu_for_the_new_role()
    {
        // 模拟"店主登录 → 登出 → 技师登录"：每次都按当前角色重新计算，互不残留。
        var ownerMenu = ShopMenu.VisibleKeys(ShopMenu.Owner);
        var technicianMenu = ShopMenu.VisibleKeys(ShopMenu.Technician);

        technicianMenu.Should().NotBeEquivalentTo(ownerMenu);
        // 技师菜单绝不能含店主/收银的页面（否则进去就 403）
        technicianMenu.Should().NotContain("pos");
        technicianMenu.Should().NotContain("stores");
    }

    [Fact]
    public void CanAccess_matches_role_matrix()
    {
        ShopMenu.CanAccess("pos", ShopMenu.Technician).Should().BeFalse();
        ShopMenu.CanAccess("queue", ShopMenu.Technician).Should().BeTrue();
        ShopMenu.CanAccess("stores", ShopMenu.Owner).Should().BeTrue();
        ShopMenu.CanAccess("stores", ShopMenu.Manager).Should().BeFalse();
        ShopMenu.CanAccess("unknown-key", ShopMenu.Owner).Should().BeFalse();
        ShopMenu.CanAccess("pos", null).Should().BeFalse();
    }
}
