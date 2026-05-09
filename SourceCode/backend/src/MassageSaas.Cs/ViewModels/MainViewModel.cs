using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Cs.ViewModels.Pos;
using MassageSaas.Shared.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace MassageSaas.Cs.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _sp;
    private readonly IApiClient _api;

    public MainViewModel(
        IServiceProvider sp,
        IApiClient api,
        SessionService session,
        AppContextService context,
        NavigationService navigation)
    {
        _sp = sp;
        _api = api;
        Session = session;
        Context = context;
        Navigation = navigation;
        BuildNav();
    }

    public SessionService Session { get; }
    public AppContextService Context { get; }
    public NavigationService Navigation { get; }

    [ObservableProperty]
    private ObservableCollection<NavItem> navItems = new();

    [ObservableProperty]
    private NavItem? selectedNavItem;

    public string CurrentUserDisplay
    {
        get
        {
            if (Session.User is null) return string.Empty;
            return $"{Session.User.RealName ?? Session.User.Username} · {RoleLabel(Session.User.Role)}";
        }
    }

    public string? SubscriptionWarning
    {
        get
        {
            if (Context.SubscriptionStatus == "Active" && Context.DaysToExpire is int d && d <= 30 && d > 0)
                return $"订阅 {d} 天后到期";
            if (Context.SubscriptionStatus is not null and not "Active")
                return "订阅已到期，仅支持只读";
            return null;
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            Context.Stores = await _api.GetStoresAsync();
            if (Context.ActiveStore is null && Context.Stores.Count > 0)
                Context.ActiveStore = Context.Stores.First();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }

        if (Session.Role is "ShopOwner" or "StoreManager")
        {
            try
            {
                var sub = await _api.GetMySubscriptionAsync();
                Context.SubscriptionStatus = sub.Status;
                Context.SubscriptionExpireAt = sub.ExpireAt;
                Context.DaysToExpire = sub.DaysToExpire;
                OnPropertyChanged(nameof(SubscriptionWarning));
            }
            catch { /* ignore */ }
        }

        if (NavItems.Count > 0) Select(NavItems.First());
    }

    private void BuildNav()
    {
        var items = new List<NavItem>();
        var role = Session.Role;
        bool canPos = role is "ShopOwner" or "StoreManager" or "Cashier";
        bool canLead = role is "ShopOwner" or "StoreManager";
        bool isOwner = role == "ShopOwner";

        if (canPos) items.Add(new NavItem("收银台", "pos", () => _sp.GetRequiredService<PosViewModel>()));
        if (canPos) items.Add(new NavItem("预约管理", "appointments", () => _sp.GetRequiredService<AppointmentsViewModel>()));
        if (canPos) items.Add(new NavItem("订单流水", "orders", () => _sp.GetRequiredService<OrdersViewModel>()));
        if (canPos) items.Add(new NavItem("房间管理", "rooms", () => _sp.GetRequiredService<RoomsViewModel>()));
        if (canPos) items.Add(new NavItem("会员管理", "members", () => _sp.GetRequiredService<MembersViewModel>()));
        items.Add(new NavItem("技师排队", "queue", () => _sp.GetRequiredService<QueueViewModel>()));
        if (canPos) items.Add(new NavItem("日报与业绩", "reports", () => _sp.GetRequiredService<ReportsViewModel>()));
        if (canPos) items.Add(new NavItem("日结/交班", "day-close", () => _sp.GetRequiredService<DayCloseViewModel>()));
        if (canLead) items.Add(new NavItem("服务项目", "services", () => _sp.GetRequiredService<ServicesViewModel>()));
        if (canLead) items.Add(new NavItem("提成规则", "commissions", () => _sp.GetRequiredService<CommissionsViewModel>()));
        if (canLead) items.Add(new NavItem("员工管理", "staff", () => _sp.GetRequiredService<StaffViewModel>()));
        if (isOwner) items.Add(new NavItem("门店管理", "stores", () => _sp.GetRequiredService<StoresViewModel>()));
        if (isOwner) items.Add(new NavItem("订阅状态", "subscription", () => _sp.GetRequiredService<SubscriptionViewModel>()));

        NavItems = new ObservableCollection<NavItem>(items);
    }

    [RelayCommand]
    private void Select(NavItem? item)
    {
        if (item is null) return;
        foreach (var n in NavItems) n.IsSelected = false;
        item.IsSelected = true;
        SelectedNavItem = item;
        Navigation.NavigateTo(item.Factory());
    }

    [RelayCommand]
    private void SwitchStore(StoreDto? store)
    {
        if (store is null) return;
        Context.ActiveStore = store;
        if (SelectedNavItem is { } cur) Select(cur);
    }

    [RelayCommand]
    private void Logout()
    {
        Session.SignOut();
        App.ShowLogin();
        App.Current.Windows.OfType<MainWindow>().FirstOrDefault()?.Close();
    }

    private static string RoleLabel(string role) => role switch
    {
        "ShopOwner" => "店主",
        "StoreManager" => "店长",
        "Cashier" => "收银员",
        "Technician" => "技师",
        "PlatformAdmin" => "平台管理员",
        _ => role
    };
}
