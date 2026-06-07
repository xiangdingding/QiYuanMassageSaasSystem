using System.Windows;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Members;

namespace MassageSaas.Cs.Views;

/// <summary>引荐情况：该会员引荐的人数、累计返佣与被引荐会员清单。逻辑与 BS 端 openReferrals 一致。</summary>
public partial class MemberReferralsWindow : Window
{
    private readonly IApiClient _api;
    private readonly MemberDto _member;

    public MemberReferralsWindow(IApiClient api, MemberDto m)
    {
        InitializeComponent();
        _api = api;
        _member = m;
        HeaderText.Text = $"引荐情况：{m.Name ?? m.CardNo}";
        _ = LoadAsync();
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        try
        {
            var data = await _api.GetMemberReferralsAsync(_member.Id);
            CountRun.Text = data.ReferredCount.ToString();
            RewardRun.Text = $"¥{data.TotalRewardEarned:F2}";
            ReferredGrid.ItemsSource = data.ReferredMembers;
            var empty = data.ReferredCount == 0;
            EmptyText.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            ReferredGrid.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
        }
        catch (System.Exception ex) { ErrorReporter.Show(ex); }
    }
}
