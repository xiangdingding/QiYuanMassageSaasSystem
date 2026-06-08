using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Services;

namespace MassageSaas.Cs.ViewModels;

public partial class ServicesViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public ServicesViewModel(IApiClient api)
    {
        _api = api;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<ServiceItemDto> rows = new();

    [ObservableProperty]
    private bool includeInactive;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private ServiceItemDto? selected;

    [RelayCommand]
    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            Rows = new ObservableCollection<ServiceItemDto>(await _api.GetServicesAsync(IncludeInactive));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    partial void OnIncludeInactiveChanged(bool value) => _ = ReloadAsync();

    [RelayCommand]
    private async Task CreateAsync()
    {
        // 默认排序号 = 当前最大 + 1（与 BS openCreate 一致，新建排到最后）
        var maxSort = Rows.Select(r => r.Sort).DefaultIfEmpty(0).Max();
        var dlg = new Views.ServiceFormWindow(null, maxSort + 1) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateServiceAsync(dlg.BuildCreateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task EditAsync(ServiceItemDto? s)
    {
        if (s is null) return;
        var dlg = new Views.ServiceFormWindow(s, s.Sort) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.UpdateServiceAsync(s.Id, dlg.BuildUpdateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>停用 / 启用切换（对齐 BS toggle：以原行字段为底，仅翻转 IsActive 后 update）。</summary>
    [RelayCommand]
    private async Task ToggleActiveAsync(ServiceItemDto? s)
    {
        if (s is null) return;
        try
        {
            await _api.UpdateServiceAsync(s.Id, new UpdateServiceItemRequest(
                Name: s.Name,
                DurationMinutes: s.DurationMinutes,
                Price: s.Price,
                MemberPrice: s.MemberPrice,
                Description: s.Description,
                IsActive: !s.IsActive,
                PriceJunior: s.PriceJunior,
                PriceMaster: s.PriceMaster,
                Code: s.Code,
                Sort: s.Sort));
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
