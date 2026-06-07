using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Orders;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 转钟（更换技师）弹窗（对齐 BS OrdersView 的转钟对话框）：展示项目/原技师，选新技师（必选，排除原技师）+ 可选原因。
/// </summary>
public partial class TransferTechnicianWindow : Window
{
    private readonly long _oldTechId;

    public long? ResultTechnicianId { get; private set; }
    public string? ResultReason { get; private set; }

    public TransferTechnicianWindow(OrderItemDto item, IReadOnlyList<StaffDto> technicians)
    {
        InitializeComponent();
        _oldTechId = item.TechnicianId;
        ServiceText.Text = item.ServiceName;
        OldTechText.Text = item.TechnicianName ?? "未指派";
        // 排除原技师，避免转到同一人
        TechCombo.ItemsSource = technicians.Where(t => t.Id != item.TechnicianId).ToList();
    }

    private void Tech_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ConfirmButton.IsEnabled = TechCombo.SelectedItem is StaffDto;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (TechCombo.SelectedItem is not StaffDto tech) return;
        ResultTechnicianId = tech.Id;
        ResultReason = string.IsNullOrWhiteSpace(ReasonBox.Text) ? null : ReasonBox.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
