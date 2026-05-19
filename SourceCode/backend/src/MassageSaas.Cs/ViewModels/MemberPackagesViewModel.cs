using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.MemberPackages;
using MassageSaas.Shared.Members;
using MassageSaas.Shared.Services;

namespace MassageSaas.Cs.ViewModels;

/// <summary>会员套餐：计次卡 / 期限卡的发卡、查看与作废。</summary>
public partial class MemberPackagesViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public MemberPackagesViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<MemberPackageDto> rows = new();

    /// <summary>状态筛选：0 全部，1 生效中，2 已用完，3 已过期，4 已作废。</summary>
    [ObservableProperty]
    private int statusFilter = 1;

    [ObservableProperty]
    private bool isBusy;

    partial void OnStatusFilterChanged(int value) => _ = ReloadAsync();

    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        try
        {
            var status = StatusFilter switch
            {
                1 => "Active",
                2 => "Used",
                3 => "Expired",
                4 => "Cancelled",
                _ => null
            };
            Rows = new ObservableCollection<MemberPackageDto>(
                await _api.GetMemberPackagesAsync(storeId: sid, status: status));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (_context.ActiveStoreId is not long sid) { MessageBox.Show("未选择门店"); return; }
        try
        {
            var members = await _api.GetMembersAsync(pageSize: 200, storeId: sid);
            if (members.Items.Count == 0) { MessageBox.Show("当前门店还没有会员，请先开卡"); return; }
            var services = await _api.GetServicesAsync();

            var dlg = new Views.MemberPackageFormWindow(sid, members.Items, services);
            if (dlg.ShowDialog() != true) return;
            await _api.CreateMemberPackageAsync(dlg.BuildRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task CancelAsync(MemberPackageDto? p)
    {
        if (p is null) return;
        if (p.Status != "Active") { MessageBox.Show("仅生效中的套餐可作废"); return; }
        if (MessageBox.Show($"作废 {p.MemberName} 的「{p.Title}」？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.CancelMemberPackageAsync(p.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
