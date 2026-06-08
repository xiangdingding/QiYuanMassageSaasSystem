using System.Windows;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 撤销日结的原因输入弹窗（与开卡/退卡/转赠窗同一套样式）。
/// 必填原因，会写入审计日志；逻辑与 BS 端 revoke 的 ElMessageBox.prompt 一致。
/// </summary>
public partial class RevokeReasonWindow : Window
{
    public string Reason => ReasonBox.Text?.Trim() ?? string.Empty;

    public RevokeReasonWindow(string dateLabel)
    {
        InitializeComponent();
        Title = $"撤销 {dateLabel} 日结";
        PromptText.Text = $"将撤销 {dateLabel} 的日结记录，撤销后可重新提交。请填写撤销原因（会写入审计日志）：";
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ReasonBox.Text))
        {
            MessageBox.Show("请填写撤销原因", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
