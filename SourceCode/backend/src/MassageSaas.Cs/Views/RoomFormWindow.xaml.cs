using System.Windows;
using MassageSaas.Shared.Rooms;

namespace MassageSaas.Cs.Views;

public partial class RoomFormWindow : Window
{
    private readonly long _storeId;

    public RoomFormWindow(RoomDto? editing, long storeId)
    {
        InitializeComponent();
        _storeId = storeId;
        // 启用开关仅编辑时有意义（新建默认启用，CreateRoomRequest 不含 IsActive）
        ActiveRow.Visibility = editing is null ? Visibility.Collapsed : Visibility.Visible;
        if (editing is not null)
        {
            Title = $"编辑房间 - {editing.RoomNo}";
            RoomNoBox.Text = editing.RoomNo;
            CapacityBox.Value = editing.Capacity;
            // 历史英文值规整成中文：保存时一并清洗
            TypeBox.Text = NormalizeRoomType(editing.RoomType);
            TimedBox.IsChecked = editing.IsTimedRoom;
            HourlyRateBox.Value = (double)editing.HourlyRate;
            RemarkBox.Text = editing.Remark ?? string.Empty;
            ActiveBox.IsChecked = editing.IsActive;
        }
    }

    private static string NormalizeRoomType(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? string.Empty
            : raw.Trim().ToLowerInvariant() switch
            {
                "standard" => "标准间",
                "vip" => "VIP",
                "couple" => "情侣间",
                _ => raw.Trim()
            };

    private int Capacity => (int)CapacityBox.Value;

    private bool IsTimedRoom => TimedBox.IsChecked == true;

    private decimal HourlyRate => (decimal)HourlyRateBox.Value;

    public CreateRoomRequest BuildCreateRequest() => new(
        StoreId: _storeId,
        RoomNo: RoomNoBox.Text.Trim(),
        Capacity: Capacity,
        RoomType: string.IsNullOrWhiteSpace(TypeBox.Text) ? null : TypeBox.Text.Trim(),
        Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim(),
        IsTimedRoom: IsTimedRoom,
        HourlyRate: IsTimedRoom ? HourlyRate : 0m);

    public UpdateRoomRequest BuildUpdateRequest() => new(
        RoomNo: RoomNoBox.Text.Trim(),
        Capacity: Capacity,
        RoomType: string.IsNullOrWhiteSpace(TypeBox.Text) ? null : TypeBox.Text.Trim(),
        Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim(),
        IsActive: ActiveBox.IsChecked == true,
        IsTimedRoom: IsTimedRoom,
        HourlyRate: IsTimedRoom ? HourlyRate : 0m);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RoomNoBox.Text)) { MessageBox.Show("房间号必填"); return; }
        if (IsTimedRoom && HourlyRate <= 0)
        {
            MessageBox.Show("计时房需设置大于 0 的小时单价", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
        Close();
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
