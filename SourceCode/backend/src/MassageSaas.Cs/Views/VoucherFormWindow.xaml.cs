using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Vouchers;

namespace MassageSaas.Cs.Views;

public partial class VoucherFormWindow : Window
{
    // 无歧义大写字母数字（去 0/O/1/I/L），与 BS 端 generateCode 保持一致
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private static readonly Random Rng = new();

    public VoucherFormWindow()
    {
        InitializeComponent();
        KindBox.SelectedIndex = 0;
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        var prefix = SelectedKind() == "GroupBuy" ? "GB" : "SC";
        var sb = new StringBuilder(prefix.Length + 1 + 4 + 1 + 4);
        sb.Append(prefix).Append('-');
        for (var i = 0; i < 4; i++) sb.Append(CodeAlphabet[Rng.Next(CodeAlphabet.Length)]);
        sb.Append('-');
        for (var i = 0; i < 4; i++) sb.Append(CodeAlphabet[Rng.Next(CodeAlphabet.Length)]);
        CodeBox.Text = sb.ToString();
        CodeBox.Focus();
        CodeBox.SelectAll();
    }

    private string SelectedKind() =>
        (KindBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "GroupBuy";

    private decimal ParseDec(TextBox b, decimal fallback)
        => decimal.TryParse(b.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    private decimal? ParseNullableDec(TextBox b)
        => decimal.TryParse(b.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

    private string? Trimmed(TextBox b)
        => string.IsNullOrWhiteSpace(b.Text) ? null : b.Text.Trim();

    public CreateVoucherRequest BuildRequest()
    {
        // 互斥：radio 选满减 → 折扣率传 null；选折扣 → 面值传 0
        var useFace = ModeFaceRadio.IsChecked == true;
        return new(
            Kind: SelectedKind(),
            Code: CodeBox.Text.Trim(),
            Title: TitleBox.Text.Trim(),
            FaceValue: useFace ? ParseDec(FaceBox, 0m) : 0m,
            MinOrderAmount: ParseDec(MinBox, 0m),
            DiscountPercent: useFace ? null : ParseNullableDec(DiscountBox),
            ValidFrom: FromBox.SelectedDate?.Date,
            ExpiresAt: ExpiryBox.SelectedDate?.Date,
            Platform: Trimmed(PlatformBox),
            Remark: Trimmed(RemarkBox));
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CodeBox.Text) || string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            MessageBox.Show("券码与标题必填");
            return;
        }
        var useFace = ModeFaceRadio.IsChecked == true;
        if (useFace && ParseDec(FaceBox, 0m) <= 0m)
        {
            MessageBox.Show("满减模式下面值必须大于 0");
            return;
        }
        if (!useFace)
        {
            var pct = ParseNullableDec(DiscountBox);
            if (pct is null or <= 0m or >= 1m)
            {
                MessageBox.Show("折扣率需在 0-1 之间（如 0.9 表示 9 折）");
                return;
            }
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
