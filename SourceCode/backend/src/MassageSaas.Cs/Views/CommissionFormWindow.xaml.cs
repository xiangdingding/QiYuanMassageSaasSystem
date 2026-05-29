using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Commissions;

namespace MassageSaas.Cs.Views;

public partial class CommissionFormWindow : Window
{
    private readonly CommissionRuleDto? _editing;
    private readonly ObservableCollection<TierRow> _tiers = new();

    public CommissionFormWindow(CommissionRuleDto? editing)
    {
        InitializeComponent();
        _editing = editing;
        TiersGrid.ItemsSource = _tiers;

        if (editing is not null)
        {
            Title = "编辑提成规则";
            ServiceIdBox.Text = editing.ServiceId?.ToString() ?? string.Empty;
            ServiceIdBox.IsEnabled = false;
            TechIdBox.Text = editing.TechnicianId?.ToString() ?? string.Empty;
            TechIdBox.IsEnabled = false;
            AmountBox.Text = editing.Amount.ToString("F4", CultureInfo.InvariantCulture);
            PriorityBox.Text = editing.Priority.ToString();
            ActiveBox.IsChecked = editing.IsActive;
            foreach (ComboBoxItem item in RuleTypeBox.Items)
            {
                if ((item.Content as string) == editing.RuleType) { RuleTypeBox.SelectedItem = item; break; }
            }
            RotationAmountBox.Text = editing.RotationAmount?.ToString("F4", CultureInfo.InvariantCulture) ?? string.Empty;
            DesignationAmountBox.Text = editing.DesignationAmount?.ToString("F4", CultureInfo.InvariantCulture) ?? string.Empty;
            LoadTiers(editing.TieredRulesJson);
        }
        else
        {
            _tiers.Add(new TierRow { FromQty = 0, Amount = 0m });
        }
    }

    private long? ParseLong(TextBox b) =>
        long.TryParse(b.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
    private decimal ParseDec(TextBox b, decimal fallback) =>
        decimal.TryParse(b.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : fallback;
    private decimal? ParseDecOptional(TextBox b) =>
        string.IsNullOrWhiteSpace(b.Text) ? null
            : decimal.TryParse(b.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : (decimal?)null;
    private int ParseInt(TextBox b, int fallback) =>
        int.TryParse(b.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    private string RuleType =>
        ((RuleTypeBox.SelectedItem as ComboBoxItem)?.Content as string) ?? "FixedAmount";

    private bool IsDualType => RuleType is "FixedAmount" or "Percentage";

    private decimal EffectiveAmount()
    {
        if (IsDualType)
        {
            var r = ParseDecOptional(RotationAmountBox) ?? 0m;
            var d = ParseDecOptional(DesignationAmountBox) ?? 0m;
            return Math.Min(r, d);
        }
        return ParseDec(AmountBox, 0m);
    }

    private string? SerializeTiers()
    {
        if (RuleType != "Tiered") return null;
        var ordered = _tiers
            .OrderBy(t => t.FromQty)
            .Select(t => new { FromQty = t.FromQty, Amount = t.Amount })
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
        ServiceId: ParseLong(ServiceIdBox),
        TechnicianId: ParseLong(TechIdBox),
        RuleType: RuleType,
        Amount: EffectiveAmount(),
        TieredRulesJson: SerializeTiers(),
        Priority: ParseInt(PriorityBox, 0),
        IsActive: ActiveBox.IsChecked == true,
        AssignmentSource: null,
        RotationAmount: IsDualType ? ParseDecOptional(RotationAmountBox) : null,
        DesignationAmount: IsDualType ? ParseDecOptional(DesignationAmountBox) : null);

    public UpdateCommissionRuleRequest BuildUpdateRequest() => new(
        RuleType: RuleType,
        Amount: EffectiveAmount(),
        TieredRulesJson: SerializeTiers(),
        Priority: ParseInt(PriorityBox, 0),
        IsActive: ActiveBox.IsChecked == true,
        AssignmentSource: null,
        RotationAmount: IsDualType ? ParseDecOptional(RotationAmountBox) : null,
        DesignationAmount: IsDualType ? ParseDecOptional(DesignationAmountBox) : null);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (IsDualType)
        {
            if (ParseDecOptional(RotationAmountBox) is null || ParseDecOptional(DesignationAmountBox) is null)
            {
                MessageBox.Show("FixedAmount / Percentage 类型请同时填写 轮钟金额 与 点钟金额。");
                return;
            }
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
