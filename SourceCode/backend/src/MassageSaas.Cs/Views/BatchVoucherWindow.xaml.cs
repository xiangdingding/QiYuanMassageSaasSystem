using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Vouchers;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 批量生成同规格优惠券。自己持有 IApiClient：发请求 + 在弹窗内展示生成的 codes 让收银员复制到剪贴板。
/// </summary>
public partial class BatchVoucherWindow : Window
{
    private readonly IApiClient _api;

    /// <summary>本次会话累计是否生成过券；调用方据此决定要不要 reload 列表。</summary>
    public bool AnyGenerated { get; private set; }

    public BatchVoucherWindow(IApiClient api)
    {
        _api = api;
        InitializeComponent();
        KindBox.SelectedIndex = 1; // 默认门店优惠券：店家自发券是更常见的批量场景
    }

    private string SelectedKind() =>
        (KindBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "GroupBuy";

    private decimal ParseDec(TextBox b, decimal fallback)
        => decimal.TryParse(b.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    private string? Trimmed(TextBox b)
        => string.IsNullOrWhiteSpace(b.Text) ? null : b.Text.Trim();

    private async void Generate_Click(object sender, RoutedEventArgs e)
    {
        // "再生成一批" 模式：把视图切回表单，按钮恢复"生成"。保留上次的字段值方便快速复用规格。
        if (ResultPanel.Visibility == Visibility.Visible)
        {
            ResultPanel.Visibility = Visibility.Collapsed;
            FormPanel.Visibility = Visibility.Visible;
            GenerateButton.Content = "生成";
            CodesBox.Clear();
            return;
        }

        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            MessageBox.Show("标题必填"); return;
        }
        if (!int.TryParse(CountBox.Text, out var count) || count < 1 || count > 500)
        {
            MessageBox.Show("数量需在 1-500 之间"); return;
        }

        var useFace = ModeFaceRadio.IsChecked == true;
        decimal faceValue = 0m;
        decimal? percent = null;
        if (useFace)
        {
            faceValue = ParseDec(FaceBox, 0m);
            if (faceValue <= 0m) { MessageBox.Show("满减面值必须大于 0"); return; }
        }
        else
        {
            percent = ParseDec(DiscountBox, 0m);
            if (percent <= 0m || percent >= 1m) { MessageBox.Show("折扣率需在 0-1 之间（如 0.9）"); return; }
        }

        var req = new BatchCreateVoucherRequest(
            Kind: SelectedKind(),
            Count: count,
            Title: TitleBox.Text.Trim(),
            FaceValue: faceValue,
            MinOrderAmount: ParseDec(MinBox, 0m),
            DiscountPercent: percent,
            ValidFrom: null,
            ExpiresAt: null,
            Platform: Trimmed(PlatformBox),
            Remark: null);

        GenerateButton.IsEnabled = false;
        try
        {
            var resp = await _api.BatchCreateVouchersAsync(req);
            AnyGenerated = true;
            // 表单整体折叠 + 结果区接管 Grid.Row=0：弹窗高度由结果区自身约束，不再撑外层滚动
            FormPanel.Visibility = Visibility.Collapsed;
            ResultHeader.Text = $"已生成 {resp.Created} 张券码，可复制下方列表：";
            CodesBox.Text = string.Join(Environment.NewLine, resp.Codes);
            ResultPanel.Visibility = Visibility.Visible;
            GenerateButton.Content = "再生成一批";
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { GenerateButton.IsEnabled = true; }
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(CodesBox.Text)) return;
        try
        {
            Clipboard.SetText(CodesBox.Text);
            MessageBox.Show("已复制到剪贴板", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch
        {
            MessageBox.Show("剪贴板复制失败，请手动选中文本拷贝", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = AnyGenerated;
        Close();
    }
}
