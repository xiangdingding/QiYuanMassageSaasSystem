using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Staff;
using MassageSaas.Shared.Stores;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 员工跨店调动：永久调动或临时借调到另一门店，并可在历史里把临时借调「归还」回原店。
/// 逻辑与 BS 端 doTransfer / returnTransfer 一致。调动/归还成功后 DialogResult=true，由调用方刷新列表。
/// </summary>
public partial class StaffTransferWindow : Window
{
    private readonly IApiClient _api;
    private readonly StaffDto _staff;

    public StaffTransferWindow(IApiClient api, StaffDto staff, IReadOnlyList<StoreDto> stores)
    {
        InitializeComponent();
        _api = api;
        _staff = staff;
        HeaderText.Text = $"跨店调动：{staff.RealName ?? staff.Username}";
        CurrentStoreText.Text = stores.FirstOrDefault(s => s.Id == staff.StoreId)?.Name ?? "—";
        // 调入门店仅列其它门店（排除当前店）
        ToStoreBox.ItemsSource = stores.Where(s => s.Id != staff.StoreId).ToList();
        _ = LoadHistoryAsync();
    }

    private bool IsTemporary => KindTemporary.IsChecked == true;

    private void Kind_Changed(object sender, RoutedEventArgs e)
    {
        if (ReturnPanel is null) return;
        ReturnPanel.Visibility = IsTemporary ? Visibility.Visible : Visibility.Collapsed;
    }

    private async System.Threading.Tasks.Task LoadHistoryAsync()
    {
        try
        {
            var list = await _api.GetStaffTransfersAsync(userId: _staff.Id);
            HistoryGrid.ItemsSource = list.Select(t => new StaffTransferRow(t)).ToList();
        }
        catch (System.Exception ex) { ErrorReporter.Show(ex); }
    }

    private async void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (ToStoreBox.SelectedValue is not long toStoreId)
        {
            MessageBox.Show("请选择调入门店", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (IsTemporary && ReturnDatePicker.SelectedDate is null)
        {
            MessageBox.Show("临时借调需填预计归还日期", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            await _api.TransferStaffAsync(_staff.Id, new TransferStaffRequest(
                ToStoreId: toStoreId,
                Kind: IsTemporary ? "Temporary" : "Permanent",
                ExpectedReturnAt: IsTemporary ? ReturnDatePicker.SelectedDate : null,
                Reason: string.IsNullOrWhiteSpace(ReasonBox.Text) ? null : ReasonBox.Text.Trim()));
            DialogResult = true;
            Close();
        }
        catch (System.Exception ex) { ErrorReporter.Show(ex); }
    }

    private async void Return_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: StaffTransferRow row }) return;
        if (MessageBox.Show($"确认归还借调？该员工将调回 {row.Dto.FromStoreName}。", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.ReturnStaffTransferAsync(row.Dto.Id);
            DialogResult = true;
            Close();
        }
        catch (System.Exception ex) { ErrorReporter.Show(ex); }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}

/// <summary>调动历史行：方向/类型/状态展示 + 是否可归还（临时且生效中）。</summary>
public class StaffTransferRow
{
    public StaffTransferDto Dto { get; }
    public StaffTransferRow(StaffTransferDto dto) => Dto = dto;

    public string Direction => $"{Dto.FromStoreName} → {Dto.ToStoreName}";
    public string KindLabel => Dto.Kind == "Permanent" ? "永久" : "临时";
    public string StatusLabel => Dto.Status switch
    {
        "InEffect" => "生效中",
        "Returned" => "已归还",
        "Cancelled" => "已撤销",
        _ => Dto.Status
    };
    public bool CanReturn => Dto.Kind == "Temporary" && Dto.Status == "InEffect";
}
