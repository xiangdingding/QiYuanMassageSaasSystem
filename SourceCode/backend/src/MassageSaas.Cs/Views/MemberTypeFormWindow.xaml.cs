using System;
using System.Collections.Generic;
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
    public MemberTypeFormWindow(MemberTypeDto? editing, List<ServiceItemDto> services, int defaultSort)
    {
        InitializeComponent();
        ServiceBox.ItemsSource = services;

        if (editing is null)
        {
            // 新建默认值（与 BS openCreate 一致）：排序 = 当前最大 + 1
            SortBox.Value = defaultSort;
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
        SortBox.Value = editing.Sort;
        ServiceBox.SelectedValue = editing.ServiceItemId;
        if (editing.MinPurchaseCount is { } mpc) MinPurchaseBox.Value = mpc;
        BonusCountBox.Value = editing.BonusCount ?? 0;
        if (editing.MinRechargeAmount is { } mra) MinRechargeBox.Value = (double)mra;
        BonusAmountBox.Value = editing.BonusAmount is { } ba && ba > 0 ? (double)ba : 0;
        DiscountBox.Value = (double)editing.Discount;
        ValidDaysBox.Value = editing.ValidDays ?? 0;
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

    // ---- 取值（按卡种归零无关字段，与 BS payload 一致）----
    private string Kind => IsCountBased ? "CountBased" : "StoredValue";
    private long? ServiceItemId => ServiceBox.SelectedValue as long?;
    private int? MinPurchaseCount => IsCountBased ? (int)MinPurchaseBox.Value : null;
    private decimal? MinRechargeAmount => IsCountBased ? null : (decimal)MinRechargeBox.Value;
    private decimal Discount => (decimal)DiscountBox.Value;
    private decimal? BonusAmount => IsCountBased ? null : (decimal)BonusAmountBox.Value;
    private int? BonusCount => IsCountBased ? (int)BonusCountBox.Value : null;
    private int? ValidDays => (int)ValidDaysBox.Value is var d && d > 0 ? d : null;

    public CreateMemberTypeRequest BuildCreateRequest() => new(
        Code: CodeBox.Text.Trim(),
        Name: NameBox.Text.Trim(),
        Sort: (int)SortBox.Value,
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
        Sort: (int)SortBox.Value,
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
