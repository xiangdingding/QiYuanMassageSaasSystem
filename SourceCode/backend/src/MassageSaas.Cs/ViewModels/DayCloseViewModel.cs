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

    partial void OnPreviewChanged(DayClosePreviewDto? value)
    {
        if (value is not null) ActualCash = value.ExpectedCash;
        OnPropertyChanged(nameof(Variance));
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

        var dlg = new RevokeReasonDialog($"{row.BusinessDate:yyyy-MM-dd}");
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

/// <summary>简易撤销原因输入弹窗（仅 WPF DayClose 用，避免引入额外的对话框框架）。</summary>
internal class RevokeReasonDialog : Window
{
    private readonly System.Windows.Controls.TextBox _input;
    public string Reason => _input.Text?.Trim() ?? string.Empty;

    public RevokeReasonDialog(string dateLabel)
    {
        Title = $"撤销 {dateLabel} 日结";
        Width = 380; Height = 200;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = $"请填写撤销 {dateLabel} 日结的原因（会写入审计日志）：",
            Margin = new Thickness(0, 0, 0, 8),
            TextWrapping = TextWrapping.Wrap
        });
        _input = new System.Windows.Controls.TextBox { Height = 48, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        panel.Children.Add(_input);
        var buttons = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
        var ok = new System.Windows.Controls.Button { Content = "确认撤销", Width = 100, IsDefault = true };
        ok.Click += (_, _) => { DialogResult = true; Close(); };
        var cancel = new System.Windows.Controls.Button { Content = "取消", Width = 80, Margin = new Thickness(8, 0, 0, 0), IsCancel = true };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        Content = panel;
    }
}
