using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Members;

namespace MassageSaas.Cs.ViewModels;

public partial class MembersViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public MembersViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<MemberDto> rows = new();

    [ObservableProperty]
    private string keyword = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private MemberDto? selected;

    [RelayCommand]
    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            var data = await _api.GetMembersAsync(
                page: 1, pageSize: 100,
                keyword: string.IsNullOrWhiteSpace(Keyword) ? null : Keyword,
                storeId: _context.ActiveStoreId);
            Rows = new ObservableCollection<MemberDto>(data.Items);
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RechargeAsync(MemberDto? m)
    {
        if (m is null) return;
        var dlg = new Views.RechargeWindow(m);
        if (dlg.ShowDialog() != true) return;

        try
        {
            await _api.RechargeAsync(new RechargeRequest(
                MemberId: m.Id,
                Amount: dlg.Amount,
                BonusAmount: dlg.BonusAmount,
                PayMethod: dlg.PayMethod,
                Remark: dlg.Remark));
            MessageBox.Show($"充值成功，到账 ¥{(dlg.Amount + dlg.BonusAmount):F2}", "提示");
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (_context.ActiveStoreId is null) { MessageBox.Show("未选择门店"); return; }
        var dlg = new Views.MemberFormWindow(null, _context.ActiveStoreId.Value);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateMemberAsync(dlg.BuildCreateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task EditAsync(MemberDto? m)
    {
        if (m is null) return;
        var dlg = new Views.MemberFormWindow(m, m.StoreId);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.UpdateMemberAsync(m.Id, dlg.BuildUpdateRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
