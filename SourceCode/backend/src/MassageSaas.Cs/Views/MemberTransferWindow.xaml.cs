using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Members;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 转赠：把当前会员的全部余额转到目标会员，原卡关闭。二选一：转给已有会员，或一并新建目标会员。
/// 逻辑与 BS 端 doTransfer / searchTarget 一致。
/// </summary>
public partial class MemberTransferWindow : Window
{
    private readonly IApiClient _api;
    private readonly MemberDto _source;
    private readonly long? _storeId;

    public MemberTransferWindow(IApiClient api, MemberDto source, long? storeId)
    {
        InitializeComponent();
        _api = api;
        _source = source;
        _storeId = storeId;
        HeaderText.Text = $"转赠：{source.Name ?? source.CardNo}（{source.CardNo}）";
        BalanceText.Text = $"¥{source.Balance:F2}";
    }

    public TransferMemberRequest? Request { get; private set; }

    private void Mode_Changed(object sender, RoutedEventArgs e)
    {
        if (ExistingPanel is null || NewPanel is null) return; // 初始化期间事件早于子元素就绪
        var existing = ModeExisting.IsChecked == true;
        ExistingPanel.Visibility = existing ? Visibility.Visible : Visibility.Collapsed;
        NewPanel.Visibility = existing ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        var k = (QueryBox.Text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(k)) return;
        try
        {
            var r = await _api.GetMembersAsync(keyword: k, page: 1, pageSize: 10, storeId: _storeId);
            // 排除自己、只列可用卡
            var candidates = r.Items.Where(m => m.Id != _source.Id && m.IsActive).ToList();
            CandidateList.ItemsSource = candidates;
            if (candidates.Count == 0)
                MessageBox.Show("没有匹配的可用会员", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (System.Exception ex) { ErrorReporter.Show(ex); }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var reason = string.IsNullOrWhiteSpace(ReasonBox.Text) ? null : ReasonBox.Text.Trim();

        if (ModeExisting.IsChecked == true)
        {
            if (CandidateList.SelectedItem is not MemberDto target)
            {
                MessageBox.Show("请先查找并选择目标会员", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Request = new TransferMemberRequest(
                ToMemberId: target.Id,
                NewMemberCardNo: null, NewMemberPhone: null, NewMemberName: null,
                Reason: reason);
        }
        else
        {
            var cardNo = NewCardNoBox.Text.Trim();
            var phone = NewPhoneBox.Text.Trim();
            if (string.IsNullOrEmpty(cardNo) || string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("新会员卡号和手机号必填", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Request = new TransferMemberRequest(
                ToMemberId: null,
                NewMemberCardNo: cardNo,
                NewMemberPhone: phone,
                NewMemberName: string.IsNullOrWhiteSpace(NewNameBox.Text) ? null : NewNameBox.Text.Trim(),
                Reason: reason);
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
