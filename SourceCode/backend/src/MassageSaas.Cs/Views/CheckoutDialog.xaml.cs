using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 结账弹窗（对齐 BS 端 CheckoutDialog.vue）：选支付方式、看应收/优惠券/会员余额、录实收金额与备注，确认后回收银台落单。
/// 校验规则与 BS 一致：会员卡支付要求余额≥应收；现金要求实收≥应收，否则「确认结账」禁用。
/// </summary>
public partial class CheckoutDialog : Window
{
    private readonly decimal _total;
    private readonly decimal _payable;
    private readonly bool _hasMember;
    private readonly decimal _memberBalance;

    /// <summary>确认后由调用方读取：支付方式 / 实收（仅现金，其它为 null）/ 备注。</summary>
    public string ResultPayMethod { get; private set; } = "Cash";
    public decimal? ResultPaidAmount { get; private set; }
    public string? ResultRemark { get; private set; }

    public CheckoutDialog(decimal total, decimal payable, bool hasMember, decimal memberBalance,
        string? voucherCode, decimal voucherDiscount)
    {
        InitializeComponent();
        _total = total;
        _payable = payable;
        _hasMember = hasMember;
        _memberBalance = memberBalance;

        PayableText.Text = $"¥ {payable:F2}";
        if (total != payable)
        {
            TotalHintText.Text = $"（合计 ¥{total:F2}）";
            TotalHintText.Visibility = Visibility.Visible;
        }

        if (voucherDiscount > 0)
        {
            VoucherRow.Visibility = Visibility.Visible;
            if (!string.IsNullOrWhiteSpace(voucherCode)) VoucherCodeText.Text = voucherCode;
            else VoucherCodeChip.Visibility = Visibility.Collapsed;
            VoucherCutText.Text = $"-¥ {voucherDiscount:F2}";
        }

        RbMember.IsEnabled = hasMember;
        MemberBalanceText.Text = $"¥ {memberBalance:F2}";

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 有会员默认走会员卡（余额不足时收银员可改其它方式），否则现金
        if (_hasMember) RbMember.IsChecked = true;
        else RbCash.IsChecked = true;
        CashBox.Text = _payable.ToString("F2", CultureInfo.InvariantCulture);
    }

    private string CurrentPayMethod =>
        RbMember.IsChecked == true ? "MemberCard" :
        RbWechat.IsChecked == true ? "Wechat" :
        RbAlipay.IsChecked == true ? "Alipay" :
        RbBank.IsChecked == true ? "BankCard" : "Cash";

    private void PayMethod_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded && CashBox is null) return;
        var method = CurrentPayMethod;
        bool isCash = method == "Cash";
        bool isMember = method == "MemberCard";

        CashRow.Visibility = isCash ? Visibility.Visible : Visibility.Collapsed;
        MemberBalanceRow.Visibility = isMember ? Visibility.Visible : Visibility.Collapsed;

        // 非现金时实收即应收（后端不会用到，但保持一致）
        if (!isCash && CashBox is not null)
            CashBox.Text = _payable.ToString("F2", CultureInfo.InvariantCulture);

        UpdateState();
    }

    private void CashBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateState();

    private decimal ParseCash() =>
        decimal.TryParse(CashBox?.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m;

    private void UpdateState()
    {
        var method = CurrentPayMethod;
        bool canSubmit = true;

        if (method == "MemberCard")
        {
            bool insufficient = _memberBalance < _payable;
            InsufficientTag.Visibility = insufficient ? Visibility.Visible : Visibility.Collapsed;
            MemberBalanceText.Foreground = insufficient
                ? System.Windows.Media.Brushes.OrangeRed
                : System.Windows.Media.Brushes.Black;
            if (insufficient) canSubmit = false;
        }

        if (method == "Cash")
        {
            var paid = ParseCash();
            var change = paid - _payable;
            if (change > 0)
            {
                ChangeText.Text = $"找零 ¥ {change:F2}";
                ChangeText.Visibility = Visibility.Visible;
            }
            else ChangeText.Visibility = Visibility.Collapsed;
            if (paid < _payable) canSubmit = false;
        }

        if (ConfirmButton is not null) ConfirmButton.IsEnabled = canSubmit;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var method = CurrentPayMethod;
        ResultPayMethod = method;
        ResultPaidAmount = method == "Cash" ? ParseCash() : null;
        ResultRemark = string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
