using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Inventory;

namespace MassageSaas.Cs.Views;

public partial class InventoryFormWindow : Window
{
    private readonly long _storeId;
    private readonly bool _isEdit;

    public InventoryFormWindow(InventoryItemDto? editing, long storeId)
    {
        InitializeComponent();
        _storeId = storeId;
        _isEdit = editing is not null;

        if (editing is not null)
        {
            Title = $"编辑 - {editing.Name}";
            CodeBox.Text = editing.Code;
            CodeBox.IsEnabled = false;
            NameBox.Text = editing.Name;
            UnitBox.Text = editing.Unit ?? string.Empty;
            // 编辑态：库存只能走出入库流水，不在此直接改
            QtyLabel.Text = "当前库存（只读，请用出入库调整）";
            QtyBox.Text = editing.Quantity.ToString("0.##", CultureInfo.InvariantCulture);
            QtyBox.IsEnabled = false;
            MinBox.Text = editing.MinQuantity.ToString("0.##", CultureInfo.InvariantCulture);
            CostBox.Text = editing.UnitCost?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;
            ActiveBox.IsChecked = editing.IsActive;
        }
        else
        {
            ActiveBox.Visibility = Visibility.Collapsed;
        }
    }

    private decimal ParseDec(TextBox b, decimal fallback)
        => decimal.TryParse(b.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    private decimal? ParseNullableDec(TextBox b)
        => decimal.TryParse(b.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

    private string? Trimmed(TextBox b)
        => string.IsNullOrWhiteSpace(b.Text) ? null : b.Text.Trim();

    public CreateInventoryItemRequest BuildCreateRequest() => new(
        StoreId: _storeId,
        Code: CodeBox.Text.Trim(),
        Name: NameBox.Text.Trim(),
        Unit: Trimmed(UnitBox),
        Quantity: ParseDec(QtyBox, 0m),
        MinQuantity: ParseDec(MinBox, 0m),
        UnitCost: ParseNullableDec(CostBox),
        Remark: null);

    public UpdateInventoryItemRequest BuildUpdateRequest() => new(
        Name: NameBox.Text.Trim(),
        Unit: Trimmed(UnitBox),
        MinQuantity: ParseDec(MinBox, 0m),
        UnitCost: ParseNullableDec(CostBox),
        Remark: null,
        IsActive: ActiveBox.IsChecked == true);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CodeBox.Text) || string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("编码与名称必填");
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
