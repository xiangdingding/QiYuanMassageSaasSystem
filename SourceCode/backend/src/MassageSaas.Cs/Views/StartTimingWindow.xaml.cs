using System.Windows;
using System.Windows.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Rooms;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 在收银台直接给一间计时房开台（不必切到房间管理页）。可按卡号/手机号关联会员，或留空按散客处理。
/// 关联会员时取命中卡作为 session.MemberId（语义按"人"判定，后续结算会校验绑定会员=结算会员）。
/// 行为与 BS 端 PosView 的开台弹窗一致。
/// </summary>
public partial class StartTimingWindow : Window
{
    private readonly IApiClient _api;
    private readonly RoomDto _room;
    private readonly long? _storeId;

    private long? _memberId;

    public StartTimingWindow(IApiClient api, RoomDto room, long? storeId)
    {
        InitializeComponent();
        _api = api;
        _room = room;
        _storeId = storeId;
        HeaderText.Text = $"开台计时：{room.RoomNo} 号房　¥{room.HourlyRate:F2}/小时";
    }

    // 输入卡号/手机号后按回车直接查询
    private void MemberKeywordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Lookup_Click(sender, e);
        }
    }

    private async void Lookup_Click(object sender, RoutedEventArgs e)
    {
        var k = (MemberKeywordBox.Text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(k)) return;
        try
        {
            var page = await _api.GetMembersAsync(keyword: k, pageSize: 5, storeId: _storeId);
            if (page.Items.Count == 0)
            {
                MessageBox.Show("未找到会员", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var m = page.Items[0];
            _memberId = m.Id;
            MemberLabel.Text = $"已关联：{m.Name ?? m.CardNo}（{m.Phone}）";
            // 关联会员后散客姓名无意义，隐藏
            CustomerPanel.Visibility = Visibility.Collapsed;
        }
        catch (System.Exception ex) { ErrorReporter.Show(ex); }
    }

    private void ClearMember_Click(object sender, RoutedEventArgs e)
    {
        _memberId = null;
        MemberKeywordBox.Text = string.Empty;
        MemberLabel.Text = "未关联（散客）";
        CustomerPanel.Visibility = Visibility.Visible;
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StartButton.IsEnabled = false;
            // 选了会员就不再传散客姓名（后端按会员名展示）；纯散客分支才用 CustomerName
            var customerName = _memberId is null
                ? (string.IsNullOrWhiteSpace(CustomerNameBox.Text) ? null : CustomerNameBox.Text.Trim())
                : null;
            var remark = string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim();
            await _api.StartTimedRoomAsync(_room.Id, new StartTimedRoomRequest(_memberId, customerName, remark));
            DialogResult = true;
            Close();
        }
        catch (System.Exception ex)
        {
            ErrorReporter.Show(ex);
            StartButton.IsEnabled = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
