using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using MassageSaas.Shared.Members;
using MassageSaas.Shared.Services;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 新增/编辑会员类型。按卡种切换字段：计次卡填「绑定服务 + 最低购买次数 + 赠送次数」，
/// 充值卡填「最低充值金额 + 赠送金额」。类型一经创建不可修改。逻辑与 BS 端 saveForm 一致。
/// </summary>
public partial class MemberTypeFormWindow : Window
{
    private readonly bool _isEdit;

    public MemberTypeFormWindow(MemberTypeDto? editing, List<ServiceItemDto> services, int defaultSort)
    {
        InitializeComponent();
        ServiceBox.ItemsSource = services;
        _isEdit = editing is not null;

        if (editing is null)
        {
            SortBox.Text = defaultSort.ToString(CultureInfo.InvariantCulture);
            UpdatePanels();
            return;
        }

        Title = $"编辑会员类型 - {editing.Name}";
        // 类型创建后不可修改
        KindStored.IsChecked = editing.Kind == "StoredValue";
        KindCount.IsChecked = editing.Kind == "CountBased";
        KindStored.IsEnabled = false;
        KindCount.IsEnabled = false;

        CodeBox.Text = editing.Code;
        NameBox.Text = editing.Name;
        SortBox.Text = editing.Sort.ToString(CultureInfo.InvariantCulture);
        ServiceBox.SelectedValue = editing.ServiceItemId;
        MinPurchaseBox.Text = editing.MinPurchaseCount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        BonusCountBox.Text = (editing.BonusCount ?? 0).ToString(CultureInfo.InvariantCulture);
        MinRechargeBox.Text = editing.MinRechargeAmount?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;
        BonusAmountBox.Text = editing.BonusAmount is { } ba && ba > 0
            ? ba.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
        DiscountBox.Text = editing.Discount.ToString("0.##", CultureInfo.InvariantCulture);
        ValidDaysBox.Text = (editing.ValidDays ?? 0).ToString(CultureInfo.InvariantCulture);
        ActiveBox.IsChecked = editing.IsActive;
        RemarkBox.Text = editing.Remark ?? string.Empty;
        UpdatePanels();
    }

    private bool IsCountBased => KindCount.IsChecked == true;

    private void Kind_Changed(object sender, RoutedEventArgs e) => UpdatePanels();

    private void UpdatePanels()
    {
        if (CountPanel is null || StoredPanel is null) return; // 初始化早于子元素
        CountPanel.Visibility = IsCountBased ? Visibility.Visible : Visibility.Collapsed;
        StoredPanel.Visibility = IsCountBased ? Visibility.Collapsed : Visibility.Visible;
    }

    // ---- 解析助手 ----
    private static int ParseInt(string? s) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static decimal ParseDecimal(string? s) =>
        decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m;

    private string Kind => IsCountBased ? "CountBased" : "StoredValue";
    private long? ServiceItemId => ServiceBox.SelectedValue as long?;
    private int? MinPurchaseCount => IsCountBased ? ParseInt(MinPurchaseBox.Text) : null;
    private decimal? MinRechargeAmount => IsCountBased ? null : ParseDecimal(MinRechargeBox.Text);
    private decimal Discount => ParseDecimal(DiscountBox.Text);
    private decimal? BonusAmount => IsCountBased ? null : ParseDecimal(BonusAmountBox.Text); // 空→0
    private int? BonusCount => IsCountBased ? ParseInt(BonusCountBox.Text) : null;
    private int? ValidDays => ParseInt(ValidDaysBox.Text) is var d && d > 0 ? d : null;

    public CreateMemberTypeRequest BuildCreateRequest() => new(
        Code: CodeBox.Text.Trim(),
        Name: NameBox.Text.Trim(),
        Sort: ParseInt(SortBox.Text),
        Kind: Kind,
        ServiceItemId: IsCountBased ? ServiceItemId : null,
        MinRechargeAmount: MinRechargeAmount,
        MinPurchaseCount: MinPurchaseCount,
        Discount: Discount,
        BonusAmount: BonusAmount,
        BonusCount: BonusCount,
        ValidDays: ValidDays,
        IsActive: ActiveBox.IsChecked == true,
        Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim());

    public UpdateMemberTypeRequest BuildUpdateRequest() => new(
        Code: CodeBox.Text.Trim(),
        Name: NameBox.Text.Trim(),
        Sort: ParseInt(SortBox.Text),
        ServiceItemId: IsCountBased ? ServiceItemId : null,
        MinRechargeAmount: MinRechargeAmount,
        MinPurchaseCount: MinPurchaseCount,
        Discount: Discount,
        BonusAmount: BonusAmount,
        BonusCount: BonusCount,
        ValidDays: ValidDays,
        IsActive: ActiveBox.IsChecked == true,
        Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim());

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CodeBox.Text)) { Warn("请输入编码"); return; }
        if (string.IsNullOrWhiteSpace(NameBox.Text)) { Warn("请输入名称"); return; }
        if (IsCountBased)
        {
            if (ServiceItemId is null) { Warn("计次卡必须选择绑定服务"); return; }
            if ((MinPurchaseCount ?? 0) <= 0) { Warn("计次卡必须设置最低购买次数"); return; }
        }
        else
        {
            if ((MinRechargeAmount ?? 0m) <= 0m) { Warn("充值卡必须设置最低充值金额"); return; }
        }
        if (Discount < 0.1m || Discount > 1m) { Warn("折扣需在 0.1 ~ 1 之间"); return; }

        DialogResult = true;
        Close();
    }

    private static void Warn(string msg) =>
        MessageBox.Show(msg, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
