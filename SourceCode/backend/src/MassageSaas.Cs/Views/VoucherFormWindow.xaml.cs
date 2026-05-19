using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Vouchers;

namespace MassageSaas.Cs.Views;

public partial class VoucherFormWindow : Window
{
    public VoucherFormWindow()
    {
        InitializeComponent();
        KindBox.SelectedIndex = 0;
    }

    private string SelectedKind() =>
        (KindBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "GroupBuy";

    private decimal ParseDec(TextBox b, decimal fallback)
        => decimal.TryParse(b.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    private decimal? ParseNullableDec(TextBox b)
        => decimal.TryParse(b.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

    private string? Trimmed(TextBox b)
        => string.IsNullOrWhiteSpace(b.Text) ? null : b.Text.Trim();

    public CreateVoucherRequest BuildRequest() => new(
        Kind: SelectedKind(),
        Code: CodeBox.Text.Trim(),
        Title: TitleBox.Text.Trim(),
        FaceValue: ParseDec(FaceBox, 0m),
        MinOrderAmount: ParseDec(MinBox, 0m),
        DiscountPercent: ParseNullableDec(DiscountBox),
        ValidFrom: FromBox.SelectedDate?.Date,
        ExpiresAt: ExpiryBox.SelectedDate?.Date,
        Platform: Trimmed(PlatformBox),
        Remark: Trimmed(RemarkBox));

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CodeBox.Text) || string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            MessageBox.Show("券码与标题必填");
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
