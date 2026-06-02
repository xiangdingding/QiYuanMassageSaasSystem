using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Commissions;

namespace MassageSaas.Cs.ViewModels;

public partial class CommissionsViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public CommissionsViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<CommissionRuleDto> rows = new();

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            Rows = new ObservableCollection<CommissionRuleDto>(await _api.GetCommissionRulesAsync());
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var dlg = new Views.CommissionFormWindow(null);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateCommissionRuleAsync(dlg.BuildCreateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>批量设置：服务 × 技师笛卡尔积按同一模板生成/更新通用规则。</summary>
    [RelayCommand]
    private async Task BulkSetAsync()
    {
        var dlg = new Views.BulkCommissionWindow(_api, _context.ActiveStoreId) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() == true) await ReloadAsync();
    }

    [RelayCommand]
    private async Task EditAsync(CommissionRuleDto? r)
    {
        if (r is null) return;
        var dlg = new Views.CommissionFormWindow(r);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.UpdateCommissionRuleAsync(r.Id, dlg.BuildUpdateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task RemoveAsync(CommissionRuleDto? r)
    {
        if (r is null) return;
        if (MessageBox.Show("确认删除提成规则？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning)
            != MessageBoxResult.OK) return;
        try
        {
            await _api.DeleteCommissionRuleAsync(r.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
