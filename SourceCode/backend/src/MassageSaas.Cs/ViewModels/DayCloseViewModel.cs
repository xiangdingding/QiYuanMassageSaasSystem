using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.DayCloses;

namespace MassageSaas.Cs.ViewModels;

public partial class DayCloseViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;
    private readonly SessionService _session;

    public DayCloseViewModel(IApiClient api, AppContextService context, SessionService session)
    {
        _api = api;
        _context = context;
        _session = session;
        _ = LoadAsync();
    }

    public bool CanRevoke => _session.Role is "ShopOwner" or "StoreManager";

    [ObservableProperty]
    private DateTime businessDate = DateTime.Today;

    [ObservableProperty]
    private DayClosePreviewDto? preview;

    [ObservableProperty]
    private decimal actualCash;

    [ObservableProperty]
    private string? remark;

    [ObservableProperty]
    private ObservableCollection<DayCloseDto> history = new();

    [ObservableProperty]
    private bool isBusy;

    public decimal Variance => ActualCash - (Preview?.ExpectedCash ?? 0m);

    /// <summary>营业日切日提示（HH:MM），切日 0 点时不显示。对齐 BS 切日提示。</summary>
    public string? CutoffText => Preview is { DayCloseCutoffMinutes: > 0 } p
        ? $"营业日切日 {p.DayCloseCutoffMinutes / 60:D2}:{p.DayCloseCutoffMinutes % 60:D2}"
        : null;

    /// <summary>已日结则锁定清点输入（对齐 BS 的 disabled 状态）。</summary>
    public bool CanEditCount => Preview is { AlreadyClosed: false };

    partial void OnPreviewChanged(DayClosePreviewDto? value)
    {
        if (value is not null) ActualCash = value.ExpectedCash;
        OnPropertyChanged(nameof(Variance));
        OnPropertyChanged(nameof(CutoffText));
        OnPropertyChanged(nameof(CanEditCount));
    }

    partial void OnActualCashChanged(decimal value) => OnPropertyChanged(nameof(Variance));

    partial void OnBusinessDateChanged(DateTime value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        try
        {
            Preview = await _api.GetDayClosePreviewAsync(sid, BusinessDate);
            History = new ObservableCollection<DayCloseDto>(await _api.GetDayCloseHistoryAsync(sid));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (_context.ActiveStoreId is not long sid || Preview is null) return;
        if (Preview.AlreadyClosed) { MessageBox.Show("该日已日结"); return; }
        if (Math.Abs(Variance) > 0.005m)
        {
            if (MessageBox.Show($"差额 ¥{Variance:F2}，确认提交？", "差额确认",
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        }
        IsBusy = true;
        try
        {
            await _api.SubmitDayCloseAsync(new SubmitDayCloseRequest(sid, BusinessDate, ActualCash, Remark));
            MessageBox.Show("日结已提交");
            await LoadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RevokeAsync(DayCloseDto? row)
    {
        if (row is null) return;
        if (!CanRevoke) { MessageBox.Show("仅店主/店长可撤销日结"); return; }

        var dlg = new Views.RevokeReasonWindow($"{row.BusinessDate:yyyy-MM-dd}")
        {
            Owner = Application.Current?.MainWindow
        };
        if (dlg.ShowDialog() != true) return;
        var reason = dlg.Reason;
        if (string.IsNullOrWhiteSpace(reason)) { MessageBox.Show("请填写撤销原因"); return; }

        IsBusy = true;
        try
        {
            await _api.RevokeDayCloseAsync(row.Id, new RevokeDayCloseRequest(reason));
            MessageBox.Show("已撤销，可重新提交日结");
            await LoadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }
}
