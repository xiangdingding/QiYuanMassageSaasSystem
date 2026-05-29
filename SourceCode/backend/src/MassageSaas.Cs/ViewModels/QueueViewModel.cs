using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Queue;

namespace MassageSaas.Cs.ViewModels;

public partial class QueueViewModel : ObservableObject, IDisposable
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;
    private readonly SessionService _session;
    private readonly ISpeechAnnouncer _speech;
    private readonly DispatcherTimer _timer;

    public QueueViewModel(IApiClient api, AppContextService context, SessionService session, ISpeechAnnouncer speech)
    {
        _api = api;
        _context = context;
        _session = session;
        _speech = speech;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _timer.Tick += async (_, _) => await ReloadAsync();
        _timer.Start();
        _ = ReloadAsync();
    }

    public bool CanManage => _session.Role is "ShopOwner" or "StoreManager" or "Cashier";

    [ObservableProperty]
    private ObservableCollection<TechnicianQueueItemDto> rows = new();

    [ObservableProperty]
    private string? lastCalled;

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        try
        {
            var data = await _api.GetQueueAsync(sid);
            Rows = new ObservableCollection<TechnicianQueueItemDto>(data);
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task SetStateAsync(object[]? args)
    {
        if (args is null || args.Length < 2) return;
        if (args[0] is not TechnicianQueueItemDto item) return;
        if (args[1] is not string state) return;

        try
        {
            await _api.SetQueueStateAsync(item.TechnicianId, new SetQueueStateRequest(state));
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task CallNextAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        try
        {
            var r = await _api.CallNextAsync(new CallNextRequest(sid));
            if (r.TechnicianId is null)
            {
                LastCalled = "目前无在岗技师";
                _speech.SayAsync("目前无在岗技师");
            }
            else
            {
                LastCalled = $"刚叫到：{r.EmployeeNo} 号 · {r.TechnicianName}";
                _speech.Say($"请 {r.EmployeeNo} 号，{r.TechnicianName} 技师上钟");
                await ReloadAsync();
            }
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task ResetDayAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        if (MessageBox.Show("确认重置今日所有技师轮次？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.ResetQueueDayAsync(sid);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    public void Dispose() => _timer.Stop();
}
