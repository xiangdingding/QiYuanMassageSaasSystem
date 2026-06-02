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

    /// <summary>退卡：退还余额并关闭该卡。逻辑与 BS 端 doRefund 一致。</summary>
    [RelayCommand]
    private async Task RefundAsync(MemberDto? m)
    {
        if (m is null) return;
        var dlg = new Views.MemberRefundWindow(m) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.RefundMemberAsync(m.Id, new RefundMemberRequest(dlg.RefundAmount, dlg.RefundMethod, dlg.Reason));
            MessageBox.Show($"已退卡，退还 ¥{dlg.RefundAmount:F2}", "提示");
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>转赠：把当前卡全部余额转到已有/新建会员，原卡关闭。逻辑与 BS 端 doTransfer 一致。</summary>
    [RelayCommand]
    private async Task TransferAsync(MemberDto? m)
    {
        if (m is null) return;
        var dlg = new Views.MemberTransferWindow(_api, m, _context.ActiveStoreId) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true || dlg.Request is null) return;
        try
        {
            await _api.TransferMemberAsync(m.Id, dlg.Request);
            MessageBox.Show("已转赠", "提示");
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>会员流水：资金流水 + 消费记录。</summary>
    [RelayCommand]
    private void ShowHistory(MemberDto? m)
    {
        if (m is null) return;
        new Views.MemberHistoryWindow(_api, m) { Owner = Application.Current?.MainWindow }.ShowDialog();
    }

    /// <summary>引荐情况：该会员引荐人数 / 累计返佣 / 被引荐清单。</summary>
    [RelayCommand]
    private void ShowReferrals(MemberDto? m)
    {
        if (m is null) return;
        new Views.MemberReferralsWindow(_api, m) { Owner = Application.Current?.MainWindow }.ShowDialog();
    }
}
