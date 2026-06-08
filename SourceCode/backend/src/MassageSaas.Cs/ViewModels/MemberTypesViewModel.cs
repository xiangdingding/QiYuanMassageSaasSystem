using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Members;
using MassageSaas.Shared.Services;

namespace MassageSaas.Cs.ViewModels;

/// <summary>
/// 会员类型管理：定义可售卡种——充值卡（充钱进余额）/ 计次卡（绑定服务、按次扣减）。
/// 逻辑与 BS 端 MemberTypesView 一致。
/// </summary>
public partial class MemberTypesViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private List<ServiceItemDto> _services = new();

    public MemberTypesViewModel(IApiClient api)
    {
        _api = api;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<MemberTypeRowViewModel> rows = new();

    /// <summary>筛选卡种：""=全部 / StoredValue / CountBased。</summary>
    [ObservableProperty]
    private string filterKind = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            // 服务项目用于计次卡绑定下拉，懒加载一次
            if (_services.Count == 0)
            {
                try { _services = (await _api.GetServicesAsync(false)).ToList(); }
                catch { /* 不阻断列表 */ }
            }
            var kind = string.IsNullOrEmpty(FilterKind) ? null : FilterKind;
            var data = await _api.GetMemberTypesAsync(includeInactive: true, kind: kind);
            Rows = new ObservableCollection<MemberTypeRowViewModel>(data.Select(d => new MemberTypeRowViewModel(d)));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    partial void OnFilterKindChanged(string value) => _ = ReloadAsync();

    [RelayCommand]
    private async Task CreateAsync()
    {
        // 默认排序号 = 当前最大 + 1（与 BS 一致，新建排到最后）
        var maxSort = Rows.Select(r => r.Sort).DefaultIfEmpty(0).Max();
        var dlg = new Views.MemberTypeFormWindow(null, _services, maxSort + 1) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateMemberTypeAsync(dlg.BuildCreateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task EditAsync(MemberTypeRowViewModel? r)
    {
        if (r is null) return;
        var dlg = new Views.MemberTypeFormWindow(r.Dto, _services, r.Sort) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.UpdateMemberTypeAsync(r.Dto.Id, dlg.BuildUpdateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task RemoveAsync(MemberTypeRowViewModel? r)
    {
        if (r is null) return;
        if (MessageBox.Show(
                $"确认删除会员类型「{r.Name}」？已开出的会员卡不会受影响，但此后不能再用该类型开卡。",
                "删除会员类型", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.DeleteMemberTypeAsync(r.Dto.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
