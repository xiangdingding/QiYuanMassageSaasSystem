using System.Globalization;
using System.Windows;
using MassageSaas.Shared.Services;

namespace MassageSaas.Cs.Views;

public partial class ServiceFormWindow : Window
{
    public ServiceFormWindow(ServiceItemDto? editing)
    {
        InitializeComponent();
        if (editing is not null)
        {
            Title = $"编辑 - {editing.Name}";
            CodeBox.Text = editing.Code;
            CodeBox.IsEnabled = false;
            NameBox.Text = editing.Name;
            DurationBox.Text = editing.DurationMinutes.ToString();
            PriceBox.Text = editing.Price.ToString("F2", CultureInfo.InvariantCulture);
            MemberPriceBox.Text = editing.MemberPrice.ToString("F2", CultureInfo.InvariantCulture);
            DescBox.Text = editing.Description ?? string.Empty;
            ActiveBox.IsChecked = editing.IsActive;
        }
    }

    private int ParseInt(System.Windows.Controls.TextBox b, int fallback)
        => int.TryParse(b.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;
    private decimal ParseDec(System.Windows.Controls.TextBox b, decimal fallback)
        => decimal.TryParse(b.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    public CreateServiceItemRequest BuildCreateRequest() => new(
        Code: CodeBox.Text.Trim(),
        Name: NameBox.Text.Trim(),
        DurationMinutes: ParseInt(DurationBox, 60),
        Price: ParseDec(PriceBox, 0m),
        MemberPrice: ParseDec(MemberPriceBox, 0m),
        Description: string.IsNullOrWhiteSpace(DescBox.Text) ? null : DescBox.Text.Trim(),
        IsActive: ActiveBox.IsChecked == true);

    public UpdateServiceItemRequest BuildUpdateRequest() => new(
        Name: NameBox.Text.Trim(),
        DurationMinutes: ParseInt(DurationBox, 60),
        Price: ParseDec(PriceBox, 0m),
        MemberPrice: ParseDec(MemberPriceBox, 0m),
        Description: string.IsNullOrWhiteSpace(DescBox.Text) ? null : DescBox.Text.Trim(),
        IsActive: ActiveBox.IsChecked == true);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CodeBox.Text) || string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("编码与名称必填");
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
