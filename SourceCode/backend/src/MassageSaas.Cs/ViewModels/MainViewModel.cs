using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Cs.Services.Devices;
using MassageSaas.Cs.ViewModels.Pos;
using MassageSaas.Shared.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace MassageSaas.Cs.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _sp;
    private readonly IApiClient _api;
    private readonly ISpeechAnnouncer _speech;

    public MainViewModel(
        IServiceProvider sp,
        IApiClient api,
        SessionService session,
        AppContextService context,
        NavigationService navigation,
        ISpeechAnnouncer speech,
        ICallerIdMonitor callerId,
        ICardReader cardReader)
    {
        _sp = sp;
        _api = api;
        _speech = speech;
        Session = session;
        Context = context;
        Navigation = navigation;
        BuildNav();

        // 外设事件：MainViewModel 是单例，在此统一订阅，避免转瞬即逝的页面 VM 反复挂事件
        callerId.CallReceived += OnIncomingCall;
        callerId.Start();
        cardReader.CardSwiped += OnCardSwiped;
        cardReader.Start();
    }

    // 来电显示：识别到来电语音播报号码（占位监听器不会触发，接来电盒后即生效）
    private void OnIncomingCall(object? sender, IncomingCall call)
    {
        _speech.SayAsync($"来电，号码 {call.PhoneNumber}");
    }

    // 磁条会员卡刷卡：在收银台界面时把卡号转给 PosViewModel 自动调出会员
    private void OnCardSwiped(object? sender, CardSwipe swipe)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            if (Navigation.CurrentViewModel is PosViewModel pos)
                pos.ApplyCardSwipe(swipe.CardNumber);
            else
                _speech.SayAsync("请先切换到收银台再刷卡");
        });
    }

    public SessionService Session { get; }
    public AppContextService Context { get; }
    public NavigationService Navigation { get; }

    // 收银台 VM 需要跨导航复用，否则购物车 / 已选会员 / 模式在切走再切回时会丢失。
    // 其它视图仍按 transient 重建以拿到最新数据；切换门店时清空此缓存。
    private PosViewModel? _cachedPosVm;

    [ObservableProperty]
    private ObservableCollection<NavItem> navItems = new();

    [ObservableProperty]
    private NavItem? selectedNavItem;

    public string WindowTitle =>
        SelectedNavItem is null ? "按摩店收银系统"
            : $"按摩店收银系统 - {SelectedNavItem.Title}";

    partial void OnSelectedNavItemChanged(NavItem? value)
    {
        OnPropertyChanged(nameof(WindowTitle));
        if (value is not null) _speech.SayAsync($"已切换到 {value.Title}");
    }

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
        if (canLead) items.Add(new NavItem("会员类型", "member-types", () => _sp.GetRequiredService<MemberTypesViewModel>()));
        items.Add(new NavItem("技师排队", "queue", () => _sp.GetRequiredService<QueueViewModel>()));
        if (canPos) items.Add(new NavItem("日报与业绩", "reports", () => _sp.GetRequiredService<ReportsViewModel>()));
        if (canPos) items.Add(new NavItem("日结/交班", "day-close", () => _sp.GetRequiredService<DayCloseViewModel>()));
        if (canLead) items.Add(new NavItem("服务项目", "services", () => _sp.GetRequiredService<ServicesViewModel>()));
        if (canPos) items.Add(new NavItem("优惠券", "vouchers", () => _sp.GetRequiredService<VouchersViewModel>()));
        if (canPos) items.Add(new NavItem("物耗库存", "inventory", () => _sp.GetRequiredService<InventoryViewModel>()));
        if (canPos) items.Add(new NavItem("服务评价", "reviews", () => _sp.GetRequiredService<ReviewsViewModel>()));
        if (canPos) items.Add(new NavItem("投诉处理", "complaints", () => _sp.GetRequiredService<ComplaintsViewModel>()));
        if (canLead) items.Add(new NavItem("排班与请假", "schedules", () => _sp.GetRequiredService<SchedulesViewModel>()));
        if (canLead) items.Add(new NavItem("提成规则", "commissions", () => _sp.GetRequiredService<CommissionsViewModel>()));
        if (canLead) items.Add(new NavItem("工资结算", "payroll", () => _sp.GetRequiredService<PayrollViewModel>()));
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

        object vm;
        if (item.Key == "pos")
        {
            vm = _cachedPosVm ??= (PosViewModel)item.Factory();
        }
        else
        {
            vm = item.Factory();
        }
        Navigation.NavigateTo(vm);
    }

    [RelayCommand]
    private void SwitchStore(StoreDto? store)
    {
        if (store is null) return;
        Context.ActiveStore = store;
        // 换店即换数据上下文，收银台缓存里残留的服务/技师/房间/购物车都得作废
        _cachedPosVm = null;
        if (SelectedNavItem is { } cur) Select(cur);
    }

    [RelayCommand]
    private void Logout()
    {
        Session.SignOut();
        App.ShowLogin();
        App.Current.Windows.OfType<MainWindow>().FirstOrDefault()?.Close();
    }

    [RelayCommand]
    private void NavByIndex(string? indexStr)
    {
        if (!int.TryParse(indexStr, out var i) || i < 0 || i >= NavItems.Count) return;
        Select(NavItems[i]);
    }

    [RelayCommand]
    private void FocusMembers()
    {
        var item = NavItems.FirstOrDefault(n => n.Key == "members");
        if (item is not null) { Select(item); _speech.SayAsync("会员管理"); }
    }

    [RelayCommand]
    private void NewSale()
    {
        var item = NavItems.FirstOrDefault(n => n.Key == "pos");
        if (item is null) return;
        Select(item);
        _speech.SayAsync("新订单");
    }

    [RelayCommand]
    private void RefreshCurrent()
    {
        // 当前 ViewModel 如有 RelayCommand 名为 ReloadCommand，则执行
        var current = Navigation.CurrentViewModel;
        if (current is null) return;
        var prop = current.GetType().GetProperty("ReloadCommand");
        if (prop?.GetValue(current) is System.Windows.Input.ICommand cmd && cmd.CanExecute(null))
        {
            cmd.Execute(null);
            _speech.SayAsync("已刷新");
        }
    }

    [RelayCommand]
    private void QuickCheckout()
    {
        if (Navigation.CurrentViewModel is Pos.PosViewModel pos)
        {
            if (pos.CheckoutCommand.CanExecute(null)) pos.CheckoutCommand.Execute(null);
        }
        else
        {
            _speech.SayAsync("当前不在收银台");
        }
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
