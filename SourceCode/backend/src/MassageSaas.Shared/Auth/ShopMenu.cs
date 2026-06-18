namespace MassageSaas.Shared.Auth;

/// <summary>
/// 店端（CS / BS）功能菜单的角色可见性——单一事实源。
/// CS 端 <c>MainViewModel.BuildNav</c> 与 BS 端路由 <c>meta.roles</c> 都以此为准，
/// 保证两端"同一角色看到同一组菜单"。
/// 切换账号时必须按当前角色重新计算（<see cref="VisibleKeys"/> 是纯函数，无缓存），
/// 杜绝菜单仍停留在上一个登录人权限的问题。
/// </summary>
public static class ShopMenu
{
    public const string Owner = "ShopOwner";
    public const string Manager = "StoreManager";
    public const string Cashier = "Cashier";
    public const string Technician = "Technician";

    private static readonly string[] Pos = { Owner, Manager, Cashier };
    private static readonly string[] Lead = { Owner, Manager };
    private static readonly string[] OwnerOnly = { Owner };
    private static readonly string[] All = { Owner, Manager, Cashier, Technician };

    // 菜单键 -> 允许访问的角色集合；数组顺序即菜单展示顺序。
    // 顺序与可见性与 BS 端 router/index.ts 保持一致。
    private static readonly (string Key, string[] Roles)[] Items =
    {
        ("dashboard",    Lead),
        ("pos",          Pos),
        ("appointments", Pos),
        ("orders",       Pos),
        ("rooms",        Pos),
        ("members",      Pos),
        ("member-types", Lead),
        ("queue",        All),
        ("reports",      Pos),
        ("day-close",    Pos),
        ("services",     Lead),
        ("vouchers",     Pos),
        ("inventory",    Pos),
        ("reviews",      Pos),
        ("complaints",   Pos),
        ("schedules",    Lead),
        ("commissions",  Lead),
        ("payroll",      Lead),
        ("staff",        Lead),
        ("stores",       OwnerOnly),
        ("subscription", OwnerOnly),
    };

    /// <summary>当前角色可见的菜单键（按展示顺序）。角色为空返回空集合。</summary>
    public static IReadOnlyList<string> VisibleKeys(string? role)
    {
        if (string.IsNullOrEmpty(role)) return Array.Empty<string>();
        var result = new List<string>();
        foreach (var (key, roles) in Items)
            if (Array.IndexOf(roles, role) >= 0)
                result.Add(key);
        return result;
    }

    /// <summary>指定角色是否可访问某菜单键。</summary>
    public static bool CanAccess(string key, string? role)
    {
        if (string.IsNullOrEmpty(role)) return false;
        foreach (var (k, roles) in Items)
            if (k == key)
                return Array.IndexOf(roles, role) >= 0;
        return false;
    }
}
