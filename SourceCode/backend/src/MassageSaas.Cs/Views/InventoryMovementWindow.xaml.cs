using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Inventory;

namespace MassageSaas.Cs.Views;

public partial class InventoryMovementWindow : Window
{
    public InventoryMovementWindow(string itemName)
    {
        InitializeComponent();
        ItemLabel.Text = $"耗材：{itemName}";
        KindBox.SelectedIndex = 0;
    }

    private string SelectedKind() =>
        (KindBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "PurchaseIn";

    private void KindBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeltaHint is null) return;
        DeltaHint.Text = SelectedKind() switch
        {
            "PurchaseIn" => "入库数量（填正数）",
            "Consume" => "消耗数量（填正数，自动按出库扣减）",
            "Discard" => "报损数量（填正数，自动按出库扣减）",
            _ => "盘点后差值（填带正负号的调整量）"
        };
    }

    /// <summary>把用户填的量按类型转成带符号的 Delta：入库为正，消耗/报损为负，盘点按原值。</summary>
    private decimal SignedDelta()
    {
        if (!decimal.TryParse(DeltaBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return 0m;
        return SelectedKind() switch
        {
            "PurchaseIn" => Math.Abs(v),
            "Consume" or "Discard" => -Math.Abs(v),
            _ => v
        };
    }

    public CreateMovementRequest BuildRequest(long itemId) => new(
        ItemId: itemId,
        Kind: SelectedKind(),
        Delta: SignedDelta(),
        Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim());

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (SignedDelta() == 0m)
        {
            MessageBox.Show("请输入有效的数量变化");
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
