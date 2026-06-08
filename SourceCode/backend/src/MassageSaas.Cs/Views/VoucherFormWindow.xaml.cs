using System.Text;
using System.Windows;
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
    }

    private string SelectedKind() => KindGroup.IsChecked == true ? "GroupBuy" : "StoreCoupon";

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        var prefix = SelectedKind() == "GroupBuy" ? "GB" : "SC";
        var sb = new StringBuilder();
        sb.Append(prefix).Append('-');
        for (var i = 0; i < 4; i++) sb.Append(CodeAlphabet[Rng.Next(CodeAlphabet.Length)]);
        sb.Append('-');
        for (var i = 0; i < 4; i++) sb.Append(CodeAlphabet[Rng.Next(CodeAlphabet.Length)]);
        CodeBox.Text = sb.ToString();
        CodeBox.Focus();
        CodeBox.SelectAll();
    }

    public CreateVoucherRequest BuildRequest()
    {
        // 互斥：满减 → 折扣率传 null；折扣 → 面值传 0（与 BS save 一致）
        var useFace = ModeFaceRadio.IsChecked == true;
        return new(
            Kind: SelectedKind(),
            Code: CodeBox.Text.Trim(),
            Title: TitleBox.Text.Trim(),
            FaceValue: useFace ? (decimal)FaceBox.Value : 0m,
            MinOrderAmount: (decimal)MinBox.Value,
            DiscountPercent: useFace ? null : (decimal)DiscountBox.Value,
            ValidFrom: FromBox.SelectedDate?.Date,
            ExpiresAt: ExpiryBox.SelectedDate?.Date,
            Platform: SelectedKind() == "GroupBuy" && !string.IsNullOrWhiteSpace(PlatformBox.Text)
                ? PlatformBox.Text.Trim() : null,
            Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim());
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CodeBox.Text) || string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            Warn("券码与标题必填");
            return;
        }
        var useFace = ModeFaceRadio.IsChecked == true;
        if (useFace && (decimal)FaceBox.Value <= 0m)
        {
            Warn("满减模式下面值必须大于 0");
            return;
        }
        if (!useFace)
        {
            var pct = (decimal)DiscountBox.Value;
            if (pct is <= 0m or >= 1m)
            {
                Warn("折扣率需在 0-1 之间（如 0.9 表示 9 折）");
                return;
            }
        }
        DialogResult = true;
        Close();
    }

    private static void Warn(string msg) =>
        MessageBox.Show(msg, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
