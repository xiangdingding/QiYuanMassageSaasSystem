using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Vouchers;

namespace MassageSaas.Cs.ViewModels;

/// <summary>优惠券：团购券（美团/点评）与门店券的录入、查看与作废。核销在收银台完成。</summary>
public partial class VouchersViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public VouchersViewModel(IApiClient api)
    {
        _api = api;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<VoucherDto> rows = new();

    /// <summary>状态筛选：0 全部，1 未核销，2 已核销，3 已过期，4 已作废。</summary>
    [ObservableProperty]
    private int statusFilter = 1;

    [ObservableProperty]
    private string keyword = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    partial void OnStatusFilterChanged(int value) => _ = ReloadAsync();

    [RelayCommand]
    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            var status = StatusFilter switch
            {
                1 => "Active",
                2 => "Redeemed",
                3 => "Expired",
                4 => "Cancelled",
                _ => null
            };
            Rows = new ObservableCollection<VoucherDto>(
                await _api.GetVouchersAsync(status, string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim()));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var dlg = new Views.VoucherFormWindow();
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateVoucherAsync(dlg.BuildRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task CancelAsync(VoucherDto? v)
    {
        if (v is null) return;
        if (v.Status != "Active") { MessageBox.Show("仅未核销的券可作废"); return; }
        if (MessageBox.Show($"作废券 {v.Code}（{v.Title}）？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.CancelVoucherAsync(v.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
