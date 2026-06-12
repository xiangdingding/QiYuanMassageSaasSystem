using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Commissions;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 批量设置提成规则：对「服务 × 技师」的笛卡尔积按同一份模板生成/更新通用规则。
/// 界面与逻辑对齐 BS CommissionsView 的批量弹窗：纵向表单 + 服务/技师多选下拉 + 分段规则类型 +
/// 固定金额/百分比双值（轮钟/点钟）、按时计费/阶梯单值、阶梯档位；覆盖开关 + "应用到 N 个组合"。
/// </summary>
public partial class BulkCommissionWindow : Window
{
    private readonly IApiClient _api;
    private readonly long? _storeId;
    private readonly ObservableCollection<CommissionFormWindow.TierRow> _tiers = new();
    private readonly ObservableCollection<PickItem> _services = new();
    private readonly ObservableCollection<PickItem> _techs = new();

    /// <summary>多选项：勾选状态 + 显示文案 + Id。</summary>
    public class PickItem : INotifyPropertyChanged
    {
        public long Id { get; init; }
        public string Label { get; init; } = string.Empty;
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public BulkCommissionWindow(IApiClient api, long? storeId)
    {
        InitializeComponent();
        _api = api;
        _storeId = storeId;
        _tiers.Add(new CommissionFormWindow.TierRow { FromQty = 0, Amount = 0m });
        TiersGrid.ItemsSource = _tiers;
        ServiceItems.ItemsSource = _services;
        TechItems.ItemsSource = _techs;
        UpdatePanels();
        UpdateSummaries();
        _ = LoadOptionsAsync();
    }

    private async System.Threading.Tasks.Task LoadOptionsAsync()
    {
        try
        {
            var services = await _api.GetServicesAsync(false);
            var techs = await _api.GetStaffAsync(role: "Technician", pageSize: 200, storeId: _storeId);

            foreach (var s in services)
                _services.Add(new PickItem { Id = s.Id, Label = $"{s.Code} {s.Name}" });
            foreach (var t in techs.Items)
                _techs.Add(new PickItem { Id = t.Id, Label = $"{(t.EmployeeNo?.ToString() ?? "-")} · {t.RealName ?? t.Username}" });
        }
        catch (System.Exception ex) { ErrorReporter.Show(ex); }
    }

    private string RuleType =>
        RbPercent.IsChecked == true ? "Percentage"
        : RbTiered.IsChecked == true ? "Tiered"
        : RbTimed.IsChecked == true ? "Timed"
        : "FixedAmount";

    private bool IsDual => RuleType is "FixedAmount" or "Percentage";

    private void RuleType_Changed(object sender, RoutedEventArgs e) => UpdatePanels();

    private void UpdatePanels()
    {
        if (FixedPanel is null) return; // Checked 在 InitializeComponent 阶段早于子元素创建

        var fixedV = RuleType == "FixedAmount";
        var percentV = RuleType == "Percentage";
        var singleV = RuleType is "Timed" or "Tiered";
        var tierV = RuleType == "Tiered";

        FixedLabel.Visibility = FixedPanel.Visibility = fixedV ? Visibility.Visible : Visibility.Collapsed;
        PercentLabel.Visibility = PercentPanel.Visibility = percentV ? Visibility.Visible : Visibility.Collapsed;
        SingleFieldLabel.Visibility = SinglePanel.Visibility = singleV ? Visibility.Visible : Visibility.Collapsed;
        TierFieldLabel.Visibility = TierPanel.Visibility = tierV ? Visibility.Visible : Visibility.Collapsed;

        // 单值区文案随类型变化（对齐 BS bulkAmountLabel）
        if (RuleType == "Timed") SingleFieldLabel.Text = "元/小时";
        else if (RuleType == "Tiered") SingleFieldLabel.Text = "默认数值";
    }

    // ---- 多选汇总 / 组合数 ----
    private int ServiceCount => _services.Count(s => s.IsSelected);
    private int TechCount => _techs.Count(t => t.IsSelected);
    private int PairCount => ServiceCount * TechCount;

    private void Selection_Changed(object sender, RoutedEventArgs e) => UpdateSummaries();

    private void UpdateSummaries()
    {
        if (ServiceSummary is null) return;
        SetSummary(ServiceSummary, ServiceCount, "请选择一个或多个服务", "已选 {0} 个服务");
        SetSummary(TechSummary, TechCount, "请选择一个或多个技师", "已选 {0} 个技师");

        if (PairCountText is not null)
            PairCountText.Text = $"将处理 {PairCount} 个 (服务 × 技师) 组合";

        if (ApplyButton is not null)
        {
            ApplyButton.Content = $"应用到 {PairCount} 个组合";
            ApplyButton.IsEnabled = PairCount > 0;
        }
    }

    private static readonly System.Windows.Media.Brush PlaceholderBrush =
        new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9CA3AF"));
    private static readonly System.Windows.Media.Brush TextBrush =
        new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#374151"));

    private static void SetSummary(System.Windows.Controls.TextBlock tb, int count, string empty, string fmt)
    {
        tb.Text = count == 0 ? empty : string.Format(fmt, count);
        tb.Foreground = count == 0 ? PlaceholderBrush : TextBrush;
    }

    private void SelectAllServices_Click(object sender, RoutedEventArgs e) { foreach (var s in _services) s.IsSelected = true; UpdateSummaries(); }
    private void ClearServices_Click(object sender, RoutedEventArgs e) { foreach (var s in _services) s.IsSelected = false; UpdateSummaries(); }
    private void SelectAllTechs_Click(object sender, RoutedEventArgs e) { foreach (var t in _techs) t.IsSelected = true; UpdateSummaries(); }
    private void ClearTechs_Click(object sender, RoutedEventArgs e) { foreach (var t in _techs) t.IsSelected = false; UpdateSummaries(); }

    private void Overwrite_Changed(object sender, RoutedEventArgs e)
    {
        if (OverwriteHint is null) return;
        OverwriteHint.Text = OverwriteBox.IsChecked == true
            ? "已有的“通用”规则（不含仅轮钟/仅点钟）会被更新"
            : "已存在的服务+技师组合将被跳过";
    }

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

    private decimal? RotationAmount() => RuleType switch
    {
        "FixedAmount" => (decimal)RotationFixedBox.Value,
        "Percentage" => (decimal)RotationPercentBox.Value,
        _ => null
    };

    private decimal? DesignationAmount() => RuleType switch
    {
        "FixedAmount" => (decimal)DesignationFixedBox.Value,
        "Percentage" => (decimal)DesignationPercentBox.Value,
        _ => null
    };

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
            rotation = RotationAmount();
            designation = DesignationAmount();
            if (rotation is null || designation is null) { Warn("请同时填写轮钟与点钟数值"); return; }
        }

        var amount = IsDual ? System.Math.Min(rotation ?? 0m, designation ?? 0m)
            : RuleType is "Timed" or "Tiered" ? (decimal)AmountBox.Value : 0m;

        var req = new BulkCommissionRuleRequest(
            ServiceIds: _services.Where(s => s.IsSelected).Select(s => s.Id).ToArray(),
            TechnicianIds: _techs.Where(t => t.IsSelected).Select(t => t.Id).ToArray(),
            RuleType: RuleType,
            Amount: amount,
            TieredRulesJson: SerializeTiers(),
            Priority: (int)PriorityBox.Value,
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
