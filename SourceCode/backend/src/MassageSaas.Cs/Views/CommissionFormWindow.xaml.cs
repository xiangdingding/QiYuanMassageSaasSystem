using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Commissions;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 新建 / 编辑提成规则。界面与逻辑对齐 BS CommissionsView 的表单弹窗：
/// 服务 / 技师用下拉选择（编辑时不可改，与 BS update 不回传 service/technician 一致）；
/// 规则类型分 固定金额 / 百分比 / 阶梯 / 按时计费，按类型显示对应输入区；
/// 固定金额、百分比支持轮钟 / 点钟双值，amount 用双值较小值兜底。
/// </summary>
public partial class CommissionFormWindow : Window
{
    private readonly IApiClient _api;
    private readonly long? _storeId;
    private readonly CommissionRuleDto? _editing;
    private readonly ObservableCollection<TierRow> _tiers = new();

    /// <summary>下拉项：Id 为 null 表示"全部"。</summary>
    private record Opt(long? Id, string Label);

    public CommissionFormWindow(IApiClient api, long? storeId, CommissionRuleDto? editing)
    {
        InitializeComponent();
        _api = api;
        _storeId = storeId;
        _editing = editing;
        TiersGrid.ItemsSource = _tiers;

        Title = editing is null ? "新建提成规则" : "编辑提成规则";

        if (editing is not null)
        {
            // 规则类型 → 选中对应单选，触发面板切换
            SelectRadio(editing.RuleType);
            switch (editing.RuleType)
            {
                case "FixedAmount":
                    RotationFixedBox.Value = (double)(editing.RotationAmount ?? editing.Amount);
                    DesignationFixedBox.Value = (double)(editing.DesignationAmount ?? editing.Amount);
                    break;
                case "Percentage":
                    RotationPercentBox.Value = (double)(editing.RotationAmount ?? editing.Amount);
                    DesignationPercentBox.Value = (double)(editing.DesignationAmount ?? editing.Amount);
                    break;
                default:
                    AmountBox.Value = (double)editing.Amount;
                    break;
            }
            PriorityBox.Value = editing.Priority;
            ActiveBox.IsChecked = editing.IsActive;
            LoadTiers(editing.TieredRulesJson);
        }
        else
        {
            _tiers.Add(new TierRow { FromQty = 0, Amount = 0m });
        }

        _ = LoadLookupsAsync();
    }

    private async System.Threading.Tasks.Task LoadLookupsAsync()
    {
        try
        {
            var services = await _api.GetServicesAsync(false);
            var techs = await _api.GetStaffAsync(role: "Technician", pageSize: 200, storeId: _storeId);

            var svcOpts = new List<Opt> { new(null, "全部服务") };
            svcOpts.AddRange(services.Select(s => new Opt(s.Id, $"{s.Code} {s.Name}")));
            ServiceBox.ItemsSource = svcOpts;
            ServiceBox.SelectedValue = _editing?.ServiceId; // null → 全部服务

            var techOpts = new List<Opt> { new(null, "全部技师") };
            techOpts.AddRange(techs.Items.Select(t => new Opt(t.Id, $"{(t.EmployeeNo?.ToString() ?? "-")} · {t.RealName ?? t.Username}")));
            TechBox.ItemsSource = techOpts;
            TechBox.SelectedValue = _editing?.TechnicianId;

            // 编辑时服务 / 技师不可改（与 BS update 一致）
            if (_editing is not null)
            {
                ServiceBox.IsEnabled = false;
                TechBox.IsEnabled = false;
            }
        }
        catch (System.Exception ex) { ErrorReporter.Show(ex); }
    }

    private void SelectRadio(string ruleType)
    {
        RbFixed.IsChecked = ruleType == "FixedAmount";
        RbPercent.IsChecked = ruleType == "Percentage";
        RbTiered.IsChecked = ruleType == "Tiered";
        RbTimed.IsChecked = ruleType == "Timed";
    }

    private string RuleType =>
        RbPercent.IsChecked == true ? "Percentage"
        : RbTiered.IsChecked == true ? "Tiered"
        : RbTimed.IsChecked == true ? "Timed"
        : "FixedAmount";

    private bool IsDual => RuleType is "FixedAmount" or "Percentage";

    private void RuleType_Checked(object sender, RoutedEventArgs e) => UpdatePanels();

    private void UpdatePanels()
    {
        if (FixedPanel is null) return; // Checked 在 InitializeComponent 阶段早于子元素创建

        FixedPanel.Visibility = RuleType == "FixedAmount" ? Visibility.Visible : Visibility.Collapsed;
        PercentPanel.Visibility = RuleType == "Percentage" ? Visibility.Visible : Visibility.Collapsed;
        SinglePanel.Visibility = RuleType is "Timed" or "Tiered" ? Visibility.Visible : Visibility.Collapsed;
        TierPanel.Visibility = RuleType == "Tiered" ? Visibility.Visible : Visibility.Collapsed;

        // 单值区文案随类型变化（对齐 BS amountLabel / amountHint）
        if (RuleType == "Timed")
        {
            SingleLabel.Text = "元/小时";
            SingleHint.Text = "按服务时长比例计算";
        }
        else if (RuleType == "Tiered")
        {
            SingleLabel.Text = "默认数值";
            SingleHint.Text = "阶梯未匹配时使用";
        }
    }

    private decimal EffectiveAmount()
    {
        if (RuleType == "FixedAmount")
            return System.Math.Min((decimal)RotationFixedBox.Value, (decimal)DesignationFixedBox.Value);
        if (RuleType == "Percentage")
            return System.Math.Min((decimal)RotationPercentBox.Value, (decimal)DesignationPercentBox.Value);
        return (decimal)AmountBox.Value;
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

    private string? SerializeTiers()
    {
        if (RuleType != "Tiered") return null;
        var ordered = _tiers
            .OrderBy(t => t.FromQty)
            .Select(t => new { t.FromQty, t.Amount })
            .ToList();
        return JsonSerializer.Serialize(ordered);
    }

    private void LoadTiers(string? json)
    {
        _tiers.Clear();
        if (string.IsNullOrWhiteSpace(json))
        {
            _tiers.Add(new TierRow { FromQty = 0, Amount = 0m });
            return;
        }
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                int qty = 0;
                decimal amount = 0m;
                if (el.TryGetProperty("FromQty", out var q) || el.TryGetProperty("fromQty", out q))
                    qty = q.GetInt32();
                if (el.TryGetProperty("Amount", out var a) || el.TryGetProperty("amount", out a))
                    amount = a.GetDecimal();
                _tiers.Add(new TierRow { FromQty = qty, Amount = amount });
            }
        }
        catch { /* 旧数据格式异常时给一个空行 */ }
        if (_tiers.Count == 0) _tiers.Add(new TierRow { FromQty = 0, Amount = 0m });
    }

    private void AddTier_Click(object sender, RoutedEventArgs e)
    {
        var last = _tiers.LastOrDefault();
        _tiers.Add(new TierRow
        {
            FromQty = last is null ? 0 : last.FromQty + 10,
            Amount = last?.Amount ?? 0m
        });
    }

    private void RemoveTier_Click(object sender, RoutedEventArgs e)
    {
        if (_tiers.Count <= 1) { MessageBox.Show("至少保留一档"); return; }
        if (TiersGrid.SelectedItem is TierRow row) _tiers.Remove(row);
    }

    public CreateCommissionRuleRequest BuildCreateRequest() => new(
        ServiceId: ServiceBox.SelectedValue as long?,
        TechnicianId: TechBox.SelectedValue as long?,
        RuleType: RuleType,
        Amount: EffectiveAmount(),
        TieredRulesJson: SerializeTiers(),
        Priority: (int)PriorityBox.Value,
        IsActive: ActiveBox.IsChecked == true,
        AssignmentSource: null,
        RotationAmount: IsDual ? RotationAmount() : null,
        DesignationAmount: IsDual ? DesignationAmount() : null);

    public UpdateCommissionRuleRequest BuildUpdateRequest() => new(
        RuleType: RuleType,
        Amount: EffectiveAmount(),
        TieredRulesJson: SerializeTiers(),
        Priority: (int)PriorityBox.Value,
        IsActive: ActiveBox.IsChecked == true,
        AssignmentSource: null,
        RotationAmount: IsDual ? RotationAmount() : null,
        DesignationAmount: IsDual ? DesignationAmount() : null);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (IsDual && (RotationAmount() is null || DesignationAmount() is null))
        {
            MessageBox.Show("请同时填写轮钟与点钟数值。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true; Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    public class TierRow
    {
        public int FromQty { get; set; }
        public decimal Amount { get; set; }
    }
}
