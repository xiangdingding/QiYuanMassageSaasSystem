using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Commissions;

namespace MassageSaas.Cs.Views;

public partial class CommissionFormWindow : Window
{
    private readonly CommissionRuleDto? _editing;

    public CommissionFormWindow(CommissionRuleDto? editing)
    {
        InitializeComponent();
        _editing = editing;
        if (editing is not null)
        {
            Title = "编辑提成规则";
            ServiceIdBox.Text = editing.ServiceId?.ToString() ?? string.Empty;
            ServiceIdBox.IsEnabled = false;
            TechIdBox.Text = editing.TechnicianId?.ToString() ?? string.Empty;
            TechIdBox.IsEnabled = false;
            AmountBox.Text = editing.Amount.ToString("F4", CultureInfo.InvariantCulture);
            PriorityBox.Text = editing.Priority.ToString();
            TieredBox.Text = editing.TieredRulesJson ?? string.Empty;
            ActiveBox.IsChecked = editing.IsActive;
            foreach (ComboBoxItem item in RuleTypeBox.Items)
            {
                if ((item.Content as string) == editing.RuleType) { RuleTypeBox.SelectedItem = item; break; }
            }
        }
    }

    private long? ParseLong(TextBox b) =>
        long.TryParse(b.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
    private decimal ParseDec(TextBox b, decimal fallback) =>
        decimal.TryParse(b.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : fallback;
    private int ParseInt(TextBox b, int fallback) =>
        int.TryParse(b.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    private string RuleType =>
        ((RuleTypeBox.SelectedItem as ComboBoxItem)?.Content as string) ?? "FixedAmount";

    public CreateCommissionRuleRequest BuildCreateRequest() => new(
        ServiceId: ParseLong(ServiceIdBox),
        TechnicianId: ParseLong(TechIdBox),
        RuleType: RuleType,
        Amount: ParseDec(AmountBox, 0m),
        TieredRulesJson: string.IsNullOrWhiteSpace(TieredBox.Text) ? null : TieredBox.Text.Trim(),
        Priority: ParseInt(PriorityBox, 0),
        IsActive: ActiveBox.IsChecked == true);

    public UpdateCommissionRuleRequest BuildUpdateRequest() => new(
        RuleType: RuleType,
        Amount: ParseDec(AmountBox, 0m),
        TieredRulesJson: string.IsNullOrWhiteSpace(TieredBox.Text) ? null : TieredBox.Text.Trim(),
        Priority: ParseInt(PriorityBox, 0),
        IsActive: ActiveBox.IsChecked == true);

    private void Save_Click(object sender, RoutedEventArgs e) { DialogResult = true; Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
