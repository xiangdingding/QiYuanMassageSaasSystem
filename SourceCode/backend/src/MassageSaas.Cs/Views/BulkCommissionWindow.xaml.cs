using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Commissions;
using MassageSaas.Shared.Services;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 批量设置提成规则：对「服务 × 技师」的笛卡尔积按同一份模板生成/更新通用规则。
/// 逻辑与 BS 端 openBulk / submitBulk 一致；FixedAmount/Percentage 支持轮钟/点钟双值，Tiered 用阶梯档位。
/// </summary>
public partial class BulkCommissionWindow : Window
{
    private readonly IApiClient _api;
    private readonly long? _storeId;
    private readonly ObservableCollection<CommissionFormWindow.TierRow> _tiers = new();

    public BulkCommissionWindow(IApiClient api, long? storeId)
    {
        InitializeComponent();
        _api = api;
        _storeId = storeId;
        _tiers.Add(new CommissionFormWindow.TierRow { FromQty = 0, Amount = 0m });
        TiersGrid.ItemsSource = _tiers;
        UpdatePanels();
        _ = LoadOptionsAsync();
    }

    private async System.Threading.Tasks.Task LoadOptionsAsync()
    {
        try
        {
            var services = await _api.GetServicesAsync(false);
            var techs = await _api.GetStaffAsync(role: "Technician", pageSize: 200, storeId: _storeId);
            ServiceList.ItemsSource = services;
            TechList.ItemsSource = techs.Items;
        }
        catch (System.Exception ex) { ErrorReporter.Show(ex); }
    }

    private string RuleType => (RuleTypeBox.SelectedItem as ComboBoxItem)?.Content as string ?? "FixedAmount";
    private bool IsDual => RuleType is "FixedAmount" or "Percentage";

    private void RuleType_Changed(object sender, SelectionChangedEventArgs e) => UpdatePanels();

    private void UpdatePanels()
    {
        if (DualPanel is null) return; // 初始化早于子元素
        DualPanel.Visibility = IsDual ? Visibility.Visible : Visibility.Collapsed;
        SinglePanel.Visibility = RuleType == "Timed" ? Visibility.Visible : Visibility.Collapsed;
        TierPanel.Visibility = RuleType == "Tiered" ? Visibility.Visible : Visibility.Collapsed;
    }

    private int ServiceCount => ServiceList.SelectedItems.Count;
    private int TechCount => TechList.SelectedItems.Count;
    private int PairCount => ServiceCount * TechCount;

    private void Selection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (PairCountText is null) return;
        PairCountText.Text = $"将处理 {PairCount} 个 (服务 × 技师) 组合";
    }

    private void SelectAllServices_Click(object sender, RoutedEventArgs e) { ServiceList.SelectAll(); }
    private void ClearServices_Click(object sender, RoutedEventArgs e) { ServiceList.UnselectAll(); }
    private void SelectAllTechs_Click(object sender, RoutedEventArgs e) { TechList.SelectAll(); }
    private void ClearTechs_Click(object sender, RoutedEventArgs e) { TechList.UnselectAll(); }

    private void AddTier_Click(object sender, RoutedEventArgs e)
    {
        var last = _tiers.LastOrDefault();
        _tiers.Add(new CommissionFormWindow.TierRow
        {
            FromQty = last is null ? 0 : last.FromQty + 10,
            Amount = last?.Amount ?? 0m
        });
    }

    private void RemoveTier_Click(object sender, RoutedEventArgs e)
    {
        if (_tiers.Count <= 1) { MessageBox.Show("至少保留一档"); return; }
        if (TiersGrid.SelectedItem is CommissionFormWindow.TierRow row) _tiers.Remove(row);
    }

    private static decimal? ParseDecOptional(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null
            : decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    private static decimal ParseDec(string? s) =>
        decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;
    private static int ParseInt(string? s) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private string? SerializeTiers() =>
        RuleType != "Tiered" ? null
            : JsonSerializer.Serialize(_tiers.OrderBy(t => t.FromQty)
                .Select(t => new { t.FromQty, t.Amount }).ToList());

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (PairCount == 0) { Warn("请至少选择一个服务和一个技师"); return; }

        decimal? rotation = null, designation = null;
        if (IsDual)
        {
            rotation = ParseDecOptional(RotationBox.Text);
            designation = ParseDecOptional(DesignationBox.Text);
            if (rotation is null || designation is null) { Warn("请同时填写轮钟与点钟数值"); return; }
        }

        var amount = IsDual ? System.Math.Min(rotation ?? 0m, designation ?? 0m)
            : RuleType == "Timed" ? ParseDec(AmountBox.Text) : 0m;

        var req = new BulkCommissionRuleRequest(
            ServiceIds: ServiceList.SelectedItems.Cast<ServiceItemDto>().Select(s => s.Id).ToArray(),
            TechnicianIds: TechList.SelectedItems.Cast<StaffDto>().Select(t => t.Id).ToArray(),
            RuleType: RuleType,
            Amount: amount,
            TieredRulesJson: SerializeTiers(),
            Priority: ParseInt(PriorityBox.Text),
            IsActive: true,
            RotationAmount: IsDual ? rotation : null,
            DesignationAmount: IsDual ? designation : null,
            OverwriteExisting: OverwriteBox.IsChecked == true);

        try
        {
            ApplyButton.IsEnabled = false;
            var result = await _api.BulkCreateCommissionRulesAsync(req);
            MessageBox.Show($"已应用：新增 {result.Created}，覆盖 {result.Updated}，跳过 {result.Skipped}",
                "批量设置", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (System.Exception ex)
        {
            ErrorReporter.Show(ex);
            ApplyButton.IsEnabled = true;
        }
    }

    private static void Warn(string msg) =>
        MessageBox.Show(msg, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
