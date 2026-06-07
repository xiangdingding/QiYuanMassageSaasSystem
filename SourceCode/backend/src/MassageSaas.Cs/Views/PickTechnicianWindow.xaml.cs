using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Services;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 指派技师弹窗（对齐 BS 端 PickTechnicianDialog.vue）：点服务项目后弹出，
/// 选技师（必选）+ 上钟方式（轮钟/点钟，默认轮钟）+ 数量（1-10），确认后加入购物车。
/// </summary>
public partial class PickTechnicianWindow : Window
{
    private const int MinQty = 1;
    private const int MaxQty = 10;
    private int _qty = 1;

    /// <summary>确认后由调用方读取：技师 / 上钟方式 / 数量。</summary>
    public StaffDto? ResultTechnician { get; private set; }
    public string ResultSource { get; private set; } = "Rotation";
    public int ResultQuantity { get; private set; } = 1;

    public PickTechnicianWindow(ServiceItemDto service, IReadOnlyList<StaffDto> technicians, bool isMember)
    {
        InitializeComponent();
        Title = $"指派技师：{service.Name}";
        var unit = isMember ? service.MemberPrice : service.Price;
        UnitText.Text = $"¥ {unit:F2}";
        DurationText.Text = $"{service.DurationMinutes} 分钟";
        TechCombo.ItemsSource = technicians;
    }

    private void Tech_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ConfirmButton.IsEnabled = TechCombo.SelectedItem is StaffDto;
    }

    private void Increment_Click(object sender, RoutedEventArgs e)
    {
        if (_qty < MaxQty) { _qty++; QtyText.Text = _qty.ToString(); }
    }

    private void Decrement_Click(object sender, RoutedEventArgs e)
    {
        if (_qty > MinQty) { _qty--; QtyText.Text = _qty.ToString(); }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (TechCombo.SelectedItem is not StaffDto tech) return;
        ResultTechnician = tech;
        ResultSource = SourceDesignation.IsChecked == true ? "Designation" : "Rotation";
        ResultQuantity = _qty;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
