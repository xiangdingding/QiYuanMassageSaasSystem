using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using MassageSaas.Cs.Services;
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

        var settings = new AppSettings();
        var refitSettings = new RefitSettings(new SystemTextJsonContentSerializer(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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
                services.AddTransient<AuthMessageHandler>();

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
            })
            .Build();

        await Host.StartAsync();

        var session = Resolve<SessionService>();
        if (session.IsAuthenticated)
            ShowMain();
        else
            ShowLogin();
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
