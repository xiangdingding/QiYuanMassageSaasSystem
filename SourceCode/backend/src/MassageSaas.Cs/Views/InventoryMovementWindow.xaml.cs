using System;
using System.Windows;
using MassageSaas.Shared.Inventory;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 出入库登记。类型由调用方（入库/领用/盘点）固定，对齐 BS openMovement：
/// 入库取正、领用/报损自动取负、盘点按填入的正负值。
/// </summary>
public partial class InventoryMovementWindow : Window
{
    private readonly string _kind;

    public InventoryMovementWindow(string itemName, string kind)
    {
        InitializeComponent();
        _kind = kind;
        Title = kind switch
        {
            "PurchaseIn" => "采购入库",
            "Consume" => "领用出库",
            "Adjust" => "盘点调整",
            "Discard" => "报损",
            _ => "出入库"
        };
        ItemNameText.Text = itemName;
        DeltaHint.Text = kind switch
        {
            "Consume" or "Discard" => "填正数，系统自动取负数",
            "Adjust" => "正数=加，负数=减",
            _ => "正数表示新增库存"
        };
    }

    /// <summary>把用户填的量按类型转成带符号的 Delta：入库为正，消耗/报损为负，盘点按原值。</summary>
    private decimal SignedDelta()
    {
        var v = (decimal)DeltaBox.Value;
        return _kind switch
        {
            "PurchaseIn" => Math.Abs(v),
            "Consume" or "Discard" => -Math.Abs(v),
            _ => v
        };
    }

    public CreateMovementRequest BuildRequest(long itemId) => new(
        ItemId: itemId,
        Kind: _kind,
        Delta: SignedDelta(),
        Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim());

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (SignedDelta() == 0m)
        {
            MessageBox.Show("请输入有效的数量变化", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
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
