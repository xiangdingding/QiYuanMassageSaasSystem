using System.Windows;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Members;

namespace MassageSaas.Cs.Views;

/// <summary>会员流水：资金流水（充值/退卡/转赠/返佣）+ 消费记录两页。逻辑与 BS 端 openHistory 一致。</summary>
public partial class MemberHistoryWindow : Window
{
    private readonly IApiClient _api;
    private readonly long _memberId;

    public MemberHistoryWindow(IApiClient api, MemberDto m)
    {
        InitializeComponent();
        _api = api;
        _memberId = m.Id;
        HeaderText.Text = $"会员流水：{m.Name ?? m.CardNo}（{m.CardNo} / {m.Phone}）";
        _ = LoadAsync();
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        try
        {
            RechargeGrid.ItemsSource = await _api.GetRechargeHistoryAsync(_memberId);
            ConsumeGrid.ItemsSource = await _api.GetConsumptionHistoryAsync(_memberId);
        }
        catch (System.Exception ex) { ErrorReporter.Show(ex); }
    }
}
