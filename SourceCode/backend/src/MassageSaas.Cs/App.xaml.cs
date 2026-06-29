using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using MassageSaas.Cs.Services;
using MassageSaas.Cs.Services.Devices;
using MassageSaas.Cs.Services.Devices.EscPos;
using MassageSaas.Cs.Services.Devices.Serial;
using MassageSaas.Cs.ViewModels;
using MassageSaas.Cs.ViewModels.Pos;
using MassageSaas.Cs.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refit;

namespace MassageSaas.Cs;

public partial class App : Application
{
    public static IHost Host { get; private set; } = null!;

    public static T Resolve<T>() where T : notnull => Host.Services.GetRequiredService<T>();

    // ---------- 单实例：本机同时只允许一个运行实例，再次启动则激活已有窗口 ----------
    // 用命名 Mutex 抢占"运行权"，用命名 EventWaitHandle 让后启动者通知先启动者把窗口拉到前台。
    // 名称带固定 GUID 避免与其它程序撞名；不加 Global\ 前缀 → 作用于当前登录会话（POS 单用户足够）。
    private const string SingleInstanceMutexName = "MassageSaas.Cs.SingleInstance.{8F3C1B7A-2D6A-4F4E-9C1D-7E2A6B5C4D3E}";
    private const string ActivateSignalName = "MassageSaas.Cs.Activate.{8F3C1B7A-2D6A-4F4E-9C1D-7E2A6B5C4D3E}";
    private static Mutex? _instanceMutex;
    private static EventWaitHandle? _activateSignal;

    protected override async void OnStartup(StartupEventArgs e)
    {
        _instanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            // 已有实例在运行：通知它把窗口激活到最前，本实例直接退出（不进入任何启动流程）
            try
            {
                using var signal = EventWaitHandle.OpenExisting(ActivateSignalName);
                signal.Set();
            }
            catch { /* 先启动者尚未建好信号或已退出，忽略 */ }
            // 本实例并未持有 Mutex（仅首个实例持有），释放/置空避免 OnExit 误调 ReleaseMutex 抛异常
            _instanceMutex.Dispose();
            _instanceMutex = null;
            Shutdown();
            return;
        }

        // 本实例为首个实例：建好激活信号并起后台线程监听，收到信号即回 UI 线程拉起窗口
        StartActivationListener();

        base.OnStartup(e);

        // WPF 的 FrameworkElement.Language 默认写死 en-US，导致日历/日期控件标题显示英文（如 "March 2026"）。
        // 覆盖为当前系统区域（中文 Windows 即 zh-CN），让日历标题、月份/星期按中文显示。
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(
                XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        // Material Design 主色改为品牌绿 #2D6A4F，使日历/控件的强调色与界面主题统一
        var palette = new PaletteHelper();
        var theme = palette.GetTheme();
        var brand = (Color)ColorConverter.ConvertFromString("#2D6A4F");
        theme.SetPrimaryColor(brand);
        theme.SetSecondaryColor(brand);
        palette.SetTheme(theme);

        // ESC/POS 中文小票走 GBK，.NET 默认不带该编码，需注册代码页提供程序
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var settings = AppSettings.Load();
        var refitSettings = new RefitSettings(new SystemTextJsonContentSerializer(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // 大小写不敏感：避免 record 构造参数与 camelCase JSON 匹配不上时整条记录字段为 null
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        }));

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(settings);
                services.AddSingleton<SessionService>();
                services.AddSingleton<NavigationService>();
                services.AddSingleton<AppContextService>();
                services.AddSingleton<PreferencesService>();
                services.AddSingleton<CredentialStore>();
                services.AddSingleton<ISpeechAnnouncer, SpeechAnnouncer>();
                services.AddTransient<AuthMessageHandler>();

                // 外设：四类设备都按"配置 enabled=true → 真实驱动；否则占位"的统一模式注册。
                // 占位实现不会触发任何 IO，门店没装对应硬件也能跑。
                if (settings.Printer.Enabled)
                    services.AddSingleton<IReceiptPrinter, EscPosReceiptPrinter>();
                else
                    services.AddSingleton<IReceiptPrinter, LoggingReceiptPrinter>();

                if (settings.CustomerDisplay.Enabled)
                    services.AddSingleton<ICustomerDisplay, SerialCustomerDisplay>();
                else
                    services.AddSingleton<ICustomerDisplay, LoggingCustomerDisplay>();

                if (settings.CallerId.Enabled)
                    services.AddSingleton<ICallerIdMonitor, SerialCallerIdMonitor>();
                else
                    services.AddSingleton<ICallerIdMonitor, NullCallerIdMonitor>();

                if (settings.CardReader.Enabled)
                    services.AddSingleton<ICardReader, SerialCardReader>();
                else
                    services.AddSingleton<ICardReader, NullCardReader>();

                services.AddRefitClient<IApiClient>(refitSettings)
                    .ConfigureHttpClient(c => c.BaseAddress = new Uri(settings.ApiBaseUrl))
                    .AddHttpMessageHandler<AuthMessageHandler>();

                services.AddSingleton<UpdateService>();

                services.AddSingleton<MainViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<PosViewModel>();
                services.AddTransient<MembersViewModel>();
                services.AddTransient<MemberTypesViewModel>();
                services.AddTransient<QueueViewModel>();
                services.AddTransient<OrdersViewModel>();
                services.AddTransient<ReportsViewModel>();
                services.AddTransient<ServicesViewModel>();
                services.AddTransient<StaffViewModel>();
                services.AddTransient<CommissionsViewModel>();
                services.AddTransient<StoresViewModel>();
                services.AddTransient<SubscriptionViewModel>();
                services.AddTransient<AppointmentsViewModel>();
                services.AddTransient<RoomsViewModel>();
                services.AddTransient<DayCloseViewModel>();
                services.AddTransient<InventoryViewModel>();
                services.AddTransient<ReviewsViewModel>();
                services.AddTransient<ComplaintsViewModel>();
                services.AddTransient<SchedulesViewModel>();
                services.AddTransient<VouchersViewModel>();
                services.AddTransient<PayrollViewModel>();
            })
            .Build();

        await Host.StartAsync();

        // 启动时根据持久化偏好挂主题；后续切换通过 Changed 事件热加载/卸载
        var prefs = Resolve<PreferencesService>();
        prefs.Changed += ApplyA11yTheme;
        ApplyA11yTheme(prefs.A11yMode);

        // 启动时检测客户端升级：强制更新未完成会退出应用，此处直接 return 不再进主流程
        if (!await CheckForUpdatesAsync())
            return;

        var session = Resolve<SessionService>();
        if (session.IsAuthenticated)
            ShowMain();
        else
            ShowLogin();
    }

    /// <summary>
    /// 起后台线程监听"激活信号"。后启动的实例会 Set 此信号，本实例收到后回到 UI 线程，
    /// 把当前窗口（登录窗 / 主窗，主窗可能已最小化到托盘）显示并抢到最前。
    /// </summary>
    private static void StartActivationListener()
    {
        _activateSignal = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateSignalName);
        var thread = new Thread(() =>
        {
            while (true)
            {
                try
                {
                    _activateSignal.WaitOne();
                    Current?.Dispatcher.BeginInvoke(new Action(ActivateExistingWindow));
                }
                catch { break; }
            }
        })
        {
            IsBackground = true,
            Name = "SingleInstanceActivator"
        };
        thread.Start();
    }

    /// <summary>把已运行实例的当前窗口拉到前台；主窗若在托盘则先恢复。</summary>
    private static void ActivateExistingWindow()
    {
        var win = Current?.MainWindow
                  ?? Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);
        if (win is null) return;

        if (win is MainWindow main)
        {
            main.RestoreFromTray();   // 处理从托盘隐藏态恢复 + 还原窗口状态 + 置顶抢焦点
            return;
        }

        if (!win.IsVisible) win.Show();
        if (win.WindowState == WindowState.Minimized) win.WindowState = WindowState.Normal;
        win.Activate();
        win.Topmost = true;
        win.Topmost = false;
    }

    /// <summary>
    /// 启动检测升级：有更新则弹 <see cref="UpdateWindow"/>。返回是否继续进入应用：
    /// 强制更新时窗口已触发退出/安装，返回 false；非强制或无更新返回 true。检测失败不阻断启动。
    /// </summary>
    private static async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            var svc = Resolve<UpdateService>();
            var result = await svc.CheckAsync();
            if (!result.HasUpdate)
                return true;

            var dlg = new UpdateWindow(svc, result);
            dlg.ShowDialog();

            // 强制更新：用户要么已进入安装（应用将退出），要么点了退出 → 不继续进主流程
            return !result.ForceUpdate;
        }
        catch
        {
            // 检测失败（网络/接口异常）不应阻断启动
            return true;
        }
    }

    /// <summary>合并/卸载无障碍主题 ResourceDictionary。Dispatcher 包一层以兼容非 UI 线程触发。</summary>
    private static ResourceDictionary? _a11yTheme;
    private static void ApplyA11yTheme(PreferencesService.AppMode mode)
    {
        Current.Dispatcher.Invoke(() =>
        {
            _a11yTheme ??= new ResourceDictionary
            {
                Source = new Uri("/Themes/A11yTheme.xaml", UriKind.Relative)
            };
            var merged = Current.Resources.MergedDictionaries;
            var isMerged = merged.Contains(_a11yTheme);
            if (mode == PreferencesService.AppMode.Accessible && !isMerged) merged.Add(_a11yTheme);
            else if (mode == PreferencesService.AppMode.Normal && isMerged) merged.Remove(_a11yTheme);
        });
    }

    public static void ShowLogin()
    {
        var vm = Resolve<LoginViewModel>();
        var w = new LoginWindow { DataContext = vm };
        vm.LoginSucceeded += () =>
        {
            _sessionExpiredHandling = false;   // 重新登录成功后，下次再过期可再次处理
            ShowMain();
            w.Close();
        };
        Current.MainWindow = w;
        w.Show();
    }

    /// <summary>并发的多次 401 只处理一次（提示 + 登出 + 跳登录），避免弹多个框/开多个登录窗。</summary>
    private static bool _sessionExpiredHandling;

    /// <summary>
    /// 登录失效（401）：提示"登录已失效，请重新登录"，点确定后登出并跳回登录界面。
    /// 由 <see cref="ErrorReporter"/> 在捕获到 401 时调用；可能从后台线程触发，故包一层 Dispatcher。
    /// </summary>
    public static void HandleSessionExpired(string message)
    {
        Current?.Dispatcher.Invoke(() =>
        {
            if (_sessionExpiredHandling) return;
            _sessionExpiredHandling = true;

            MessageBox.Show(message, "登录已失效", MessageBoxButton.OK, MessageBoxImage.Warning);
            try { Resolve<SessionService>().SignOut(); } catch { /* ignore */ }

            // 先打开登录窗（成为新的 MainWindow），再关掉主窗口与残留弹窗，避免"最后一个窗口关闭即退出"
            ShowLogin();
            foreach (var win in Current.Windows.OfType<Window>().ToList())
                if (win is not LoginWindow) win.Close();
        });
    }

    public static void ShowMain()
    {
        var vm = Resolve<MainViewModel>();
        var w = new MainWindow { DataContext = vm };
        Current.MainWindow = w;
        w.Show();
        _ = vm.InitializeAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        // 释放单实例占用：Mutex 解锁让下次启动能成为首个实例；信号句柄一并清理
        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        _activateSignal?.Dispose();

        if (Host is not null)
        {
            await Host.StopAsync();
            Host.Dispose();
        }
        base.OnExit(e);
    }
}
