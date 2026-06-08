using System.Windows;
using MassageSaas.Shared.Inventory;

namespace MassageSaas.Cs.Views;

public partial class InventoryFormWindow : Window
{
    private readonly long _storeId;

    public InventoryFormWindow(InventoryItemDto? editing, long storeId)
    {
        InitializeComponent();
        _storeId = storeId;

        if (editing is null) return;

        Title = $"编辑 - {editing.Name}";
        CodeBox.Text = editing.Code;
        CodeBox.IsEnabled = false;
        NameBox.Text = editing.Name;
        UnitBox.Text = editing.Unit ?? string.Empty;
        // 编辑态：库存只能走出入库流水，不在此直接改
        QtyLabel.Text = "当前库存";
        QtyBox.Value = (double)editing.Quantity;
        QtyBox.IsEnabled = false;
        MinBox.Value = (double)editing.MinQuantity;
        if (editing.UnitCost is decimal c) CostBox.Value = (double)c;
        RemarkBox.Text = editing.Remark ?? string.Empty;
        ActiveBox.IsChecked = editing.IsActive;
        ActiveRow.Visibility = Visibility.Visible;
    }

    private decimal? CostOrNull => (decimal)CostBox.Value > 0m ? (decimal)CostBox.Value : null;
    private string? Trimmed(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public CreateInventoryItemRequest BuildCreateRequest() => new(
        StoreId: _storeId,
        Code: CodeBox.Text.Trim(),
        Name: NameBox.Text.Trim(),
        Unit: Trimmed(UnitBox.Text),
        Quantity: (decimal)QtyBox.Value,
        MinQuantity: (decimal)MinBox.Value,
        UnitCost: CostOrNull,
        Remark: Trimmed(RemarkBox.Text));

    public UpdateInventoryItemRequest BuildUpdateRequest() => new(
        Name: NameBox.Text.Trim(),
        Unit: Trimmed(UnitBox.Text),
        MinQuantity: (decimal)MinBox.Value,
        UnitCost: CostOrNull,
        Remark: Trimmed(RemarkBox.Text),
        IsActive: ActiveBox.IsChecked == true);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CodeBox.Text) || string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("编码与名称必填", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
