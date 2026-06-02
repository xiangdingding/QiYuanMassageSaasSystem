using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
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

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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

                services.AddSingleton<MainViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<PosViewModel>();
                services.AddTransient<MembersViewModel>();
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

        var session = Resolve<SessionService>();
        if (session.IsAuthenticated)
            ShowMain();
        else
            ShowLogin();
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
            ShowMain();
            w.Close();
        };
        Current.MainWindow = w;
        w.Show();
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
        if (Host is not null)
        {
            await Host.StopAsync();
            Host.Dispose();
        }
        base.OnExit(e);
    }
}
