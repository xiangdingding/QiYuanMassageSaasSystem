using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Cs.Services.Devices;
using MassageSaas.Cs.ViewModels.Pos;
using MassageSaas.Shared.Auth;
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

        // MainViewModel 是单例：登录/登出/换号都复用同一实例，必须在会话变化时
        // 按"当前登录人"重建菜单并清空上一个账号的派生状态，否则菜单会停留在
        // 前一个登录人的权限（如技师登进来却看到店主的收银台，一点就 403）。
        Session.Changed += OnSessionChanged;

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

    /// <summary>顶栏用户区：登录人显示名（真实姓名优先，回退账号）。</summary>
    public string UserName => Session.User is { } u ? (u.RealName ?? u.Username) : string.Empty;

    /// <summary>顶栏用户区：角色中文标签。</summary>
    public string UserRoleLabel => Session.User is { } u ? RoleLabel(u.Role) : string.Empty;

    /// <summary>个人设置改名后刷新顶栏展示。</summary>
    public void RefreshUser()
    {
        OnPropertyChanged(nameof(UserName));
        OnPropertyChanged(nameof(UserRoleLabel));
        OnPropertyChanged(nameof(CurrentUserDisplay));
    }

    public string? SubscriptionWarning
    {
        get
        {
            // 与 BS 版（MainLayout.vue）保持一致：Trial / Active / Expired|Disabled 三态
            var status = Context.SubscriptionStatus;
            var days = Context.DaysToExpire;
            // 只有 Expired / Disabled 才是真正"到期、仅只读"
            if (status is "Expired" or "Disabled")
                return "订阅已到期，仅支持只读";
            // 试用中：走「试用中，剩 X 天」，不再重复弹"X 天后到期"
            if (status == "Trial")
                return days is int td ? $"试用中，剩 {td} 天" : "试用中";
            // 正式订阅临近到期（30 天内）才提醒
            if (status == "Active" && days is int d && d > 0 && d <= 30)
                return $"订阅 {d} 天后到期";
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

    // 会话变化（登录成功 / 登出 / 换号）：按当前角色重建菜单，并清掉上一个账号的残留。
    private void OnSessionChanged()
    {
        var dispatcher = App.Current?.Dispatcher;
        if (dispatcher is null) { ApplySessionChange(); return; }
        dispatcher.Invoke(ApplySessionChange);
    }

    private void ApplySessionChange()
    {
        // 收银台缓存属于上一个登录人的数据上下文（购物车/会员/技师），换号必须作废
        _cachedPosVm = null;
        // 门店与订阅状态都随租户/角色变化，先清空，等 InitializeAsync 按新账号重新拉取
        Context.Stores = new();
        Context.ActiveStore = null;
        Context.SubscriptionStatus = null;
        Context.SubscriptionExpireAt = null;
        Context.DaysToExpire = null;
        BuildNav();
        RefreshUser();
        OnPropertyChanged(nameof(SubscriptionWarning));
    }

    private void BuildNav()
    {
        var items = ShopMenu.VisibleKeys(Session.Role)
            .Select(CreateNavItem)
            .OfType<NavItem>()
            .ToList();

        NavItems = new ObservableCollection<NavItem>(items);
        // 清掉旧的选中项，避免仍指向当前角色已无权访问的页面
        SelectedNavItem = null;
    }

    // 菜单可见性由 ShopMenu（跨端单一事实源）决定；这里只负责键 -> 标题 + VM 工厂的映射。
    private NavItem? CreateNavItem(string key) => key switch
    {
        "pos" => new NavItem("收银台", key, () => _sp.GetRequiredService<PosViewModel>()),
        "appointments" => new NavItem("预约管理", key, () => _sp.GetRequiredService<AppointmentsViewModel>()),
        "orders" => new NavItem("订单流水", key, () => _sp.GetRequiredService<OrdersViewModel>()),
        "rooms" => new NavItem("房间管理", key, () => _sp.GetRequiredService<RoomsViewModel>()),
        "members" => new NavItem("会员管理", key, () => _sp.GetRequiredService<MembersViewModel>()),
        "member-types" => new NavItem("会员类型", key, () => _sp.GetRequiredService<MemberTypesViewModel>()),
        "queue" => new NavItem("技师排队", key, () => _sp.GetRequiredService<QueueViewModel>()),
        "reports" => new NavItem("日报与业绩", key, () => _sp.GetRequiredService<ReportsViewModel>()),
        "day-close" => new NavItem("日结/交班", key, () => _sp.GetRequiredService<DayCloseViewModel>()),
        "services" => new NavItem("服务项目", key, () => _sp.GetRequiredService<ServicesViewModel>()),
        "vouchers" => new NavItem("优惠券", key, () => _sp.GetRequiredService<VouchersViewModel>()),
        "inventory" => new NavItem("物耗库存", key, () => _sp.GetRequiredService<InventoryViewModel>()),
        "reviews" => new NavItem("服务评价", key, () => _sp.GetRequiredService<ReviewsViewModel>()),
        "complaints" => new NavItem("投诉处理", key, () => _sp.GetRequiredService<ComplaintsViewModel>()),
        "schedules" => new NavItem("排班与请假", key, () => _sp.GetRequiredService<SchedulesViewModel>()),
        "commissions" => new NavItem("提成规则", key, () => _sp.GetRequiredService<CommissionsViewModel>()),
        "payroll" => new NavItem("工资结算", key, () => _sp.GetRequiredService<PayrollViewModel>()),
        "staff" => new NavItem("员工管理", key, () => _sp.GetRequiredService<StaffViewModel>()),
        "stores" => new NavItem("门店管理", key, () => _sp.GetRequiredService<StoresViewModel>()),
        "subscription" => new NavItem("订阅状态", key, () => _sp.GetRequiredService<SubscriptionViewModel>()),
        _ => null
    };

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
            if (_cachedPosVm is null)
            {
                // 首建：构造函数已 LoadAsync，无需重复加载
                _cachedPosVm = (PosViewModel)item.Factory();
            }
            else
            {
                // 复用缓存实例：重新进入时刷新房间/服务/技师（房间管理改动后及时可见），购物车保留
                _cachedPosVm.ReloadCommand.Execute(null);
            }
            vm = _cachedPosVm;
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
