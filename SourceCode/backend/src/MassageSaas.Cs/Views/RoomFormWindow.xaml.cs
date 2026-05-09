using System.Globalization;
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
        if (editing is not null)
        {
            Title = $"编辑房间 - {editing.RoomNo}";
            RoomNoBox.Text = editing.RoomNo;
            CapacityBox.Text = editing.Capacity.ToString();
            TypeBox.Text = editing.RoomType ?? string.Empty;
            RemarkBox.Text = editing.Remark ?? string.Empty;
            ActiveBox.IsChecked = editing.IsActive;
        }
    }

    private int Capacity =>
        int.TryParse(CapacityBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 1;

    public CreateRoomRequest BuildCreateRequest() => new(
        StoreId: _storeId,
        RoomNo: RoomNoBox.Text.Trim(),
        Capacity: Capacity,
        RoomType: string.IsNullOrWhiteSpace(TypeBox.Text) ? null : TypeBox.Text.Trim(),
        Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim());

    public UpdateRoomRequest BuildUpdateRequest() => new(
        RoomNo: RoomNoBox.Text.Trim(),
        Capacity: Capacity,
        RoomType: string.IsNullOrWhiteSpace(TypeBox.Text) ? null : TypeBox.Text.Trim(),
        Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim(),
        IsActive: ActiveBox.IsChecked == true);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RoomNoBox.Text)) { MessageBox.Show("房间号必填"); return; }
        DialogResult = true;
        Close();
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
