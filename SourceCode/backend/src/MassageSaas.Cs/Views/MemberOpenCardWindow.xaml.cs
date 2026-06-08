using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Members;
using MassageSaas.Shared.Services;
using MassageSaas.Shared.Settings;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 按会员类型开卡（对齐 BS 端开卡对话框）：手机号/卡号/姓名/性别/生日、会员类型（充值卡/计次卡）、
/// 充值金额或购买次数、支付来源、折扣（按类型只读）、引荐人（按手机号聚合搜索）、备注。
/// 折扣/赠送/到期由模板决定，按模板最低门槛校验；逻辑与 BS 端 create + memberTypeId 一致。
/// </summary>
public partial class MemberOpenCardWindow : Window
{
    private readonly IApiClient _api;
    private readonly long _storeId;
    private List<ServiceItemDto> _services = new();
    private long? _referrerMemberId;
    private long? _referrerStaffId;
    private ReferralSettingDto? _referral;

    /// <summary>员工推荐人下拉项。Display = "编号 姓名"（无编号时只姓名）。</summary>
    private sealed record StaffOption(long? Id, string Display);

    public MemberOpenCardWindow(IApiClient api, long storeId, string? presetPhone)
    {
        InitializeComponent();
        _api = api;
        _storeId = storeId;
        AmountBox.ValueChanged += Amounts_Changed;
        CountBox.ValueChanged += Amounts_Changed;
        // 开卡场景：手机号变动自动同步卡号（与 BS 端 watch(form.phone) 一致）
        PhoneBox.TextChanged += PhoneBox_TextChanged;
        if (!string.IsNullOrWhiteSpace(presetPhone))
        {
            // 加办新卡：预填并锁定手机号（触发卡号同步），姓名带过来（与 BS openCreateForPhone 一致）
            PhoneBox.Text = presetPhone;
            PhoneBox.IsEnabled = false;
            LockTip.Visibility = Visibility.Visible;
            _ = PrefillNameAsync(presetPhone);
        }
        _ = LoadAsync();
    }

    // ---- 手机号 → 卡号自动同步（对齐 BS computeNextCardNo + watch）----
    // 不足 11 位：卡号 = 手机号明文同步；满 11 位：卡号 = 手机号 + 该号已有卡数+1（两位）。
    // 用户可手动覆盖卡号，覆盖后只要手机号不再变动即保留。
    private async void PhoneBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var phone = PhoneBox.Text?.Trim() ?? "";
        if (phone.Length < 11) { CardNoBox.Text = phone; return; }
        var snapshot = phone;
        var next = await ComputeNextCardNoAsync(phone);
        if ((PhoneBox.Text?.Trim() ?? "") == snapshot) CardNoBox.Text = next; // 期间手机号又改则丢弃过期结果
    }

    private async System.Threading.Tasks.Task<string> ComputeNextCardNoAsync(string phone)
    {
        if (phone.Length != 11) return phone;
        try
        {
            var r = await _api.GetMembersAsync(keyword: phone, pageSize: 100, includeClosed: true);
            var n = r.Items.Count(m => m.Phone == phone);
            return phone + (n + 1).ToString().PadLeft(2, '0');
        }
        catch { return phone; }
    }

    /// <summary>加办时带出该手机号已有客户的姓名（对齐 BS：group.primaryName）。</summary>
    private async System.Threading.Tasks.Task PrefillNameAsync(string phone)
    {
        try
        {
            var r = await _api.GetMemberGroupedAsync(keyword: phone, includeClosed: true, storeId: _storeId);
            var g = r.Items.FirstOrDefault(x => x.Phone == phone);
            if (g?.PrimaryName is { Length: > 0 } name && string.IsNullOrWhiteSpace(NameBox.Text))
                NameBox.Text = name;
        }
        catch { /* 静默：姓名只是便利预填 */ }
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        try
        {
            _services = (await _api.GetServicesAsync(false)).ToList();
            var types = await _api.GetMemberTypesAsync(includeInactive: false);
            TypeBox.ItemsSource = types.Where(t => t.IsActive).ToList();

            // 员工推荐人下拉：本店在职员工 + "不指定"
            var staff = await _api.GetStaffAsync(pageSize: 200, storeId: _storeId);
            var opts = new List<StaffOption> { new(null, "不指定") };
            opts.AddRange(staff.Items.Where(s => s.IsActive)
                .Select(s => new StaffOption(s.Id,
                    s.EmployeeNo is int no ? $"{no} {s.RealName ?? s.Username}" : (s.RealName ?? s.Username))));
            StaffReferrerBox.ItemsSource = opts;
            StaffReferrerBox.SelectedIndex = 0;

            // 推荐规则：用于开卡时预估顾客返佣 / 员工提成（失败不致命）
            try { _referral = await _api.GetReferralSettingsAsync(); } catch { _referral = null; }
            RefreshReferralPreview();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    private MemberTypeDto? SelectedType => TypeBox.SelectedItem as MemberTypeDto;
    private bool IsCountBased => SelectedType?.Kind == "CountBased";

    /// <summary>计次卡绑定服务单价：会员价优先，为 0 回退标准价。</summary>
    private decimal BoundUnitPrice
    {
        get
        {
            var t = SelectedType;
            if (t?.ServiceItemId is not long sid) return 0m;
            var s = _services.FirstOrDefault(x => x.Id == sid);
            if (s is null) return 0m;
            return s.MemberPrice > 0 ? s.MemberPrice : s.Price;
        }
    }

    private void Type_Changed(object sender, SelectionChangedEventArgs e)
    {
        var t = SelectedType;
        if (t is null) { RulesBox.Visibility = Visibility.Collapsed; DiscountBox.Text = "—"; return; }

        // 类型规则摘要
        var kind = t.Kind == "StoredValue" ? "充值卡" : "计次卡";
        var discount = t.Discount < 1m ? $"{t.Discount * 10m:0.#} 折" : "原价";
        var threshold = t.Kind == "StoredValue"
            ? $"最低充值 ¥{t.MinRechargeAmount ?? 0m:F2}"
            : $"最少 {t.MinPurchaseCount ?? 1} 次" + (string.IsNullOrEmpty(t.ServiceItemName) ? "" : $" · 绑定 {t.ServiceItemName}");
        var bonus = t.Kind == "StoredValue"
            ? ((t.BonusAmount ?? 0m) > 0 ? $"，赠送 ¥{t.BonusAmount:F2}" : "")
            : ((t.BonusCount ?? 0) > 0 ? $"，赠送 {t.BonusCount} 次" : "");
        var expire = t.ValidDays is int d && d > 0
            ? $"，{DateTime.Now.AddDays(d):yyyy-MM-dd} 到期（{d} 天）" : "，永久有效";
        RulesText.Text = $"{kind} · {discount} · {threshold}{bonus}{expire}";
        RulesBox.Visibility = Visibility.Visible;
        DiscountBox.Text = discount;

        // 切换面板 + 预填最低值（步进器最小值同步设为门槛）
        StoredPanel.Visibility = IsCountBased ? Visibility.Collapsed : Visibility.Visible;
        CountPanel.Visibility = IsCountBased ? Visibility.Visible : Visibility.Collapsed;
        if (IsCountBased)
        {
            CountBox.Minimum = t.MinPurchaseCount ?? 1;
            CountBox.Value = t.MinPurchaseCount ?? 1;
        }
        else
        {
            AmountBox.Minimum = (double)(t.MinRechargeAmount ?? 0m);
            AmountBox.Value = (double)(t.MinRechargeAmount ?? 0m);
        }

        RefreshCalc();
    }

    private void Amounts_Changed(object? sender, EventArgs e) => RefreshCalc();

    private void RefreshCalc()
    {
        var t = SelectedType;
        if (t is null) { RefreshReferralPreview(); return; }
        var discount = t.Discount;
        if (IsCountBased)
        {
            var count = (int)CountBox.Value;
            var face = Math.Round(count * BoundUnitPrice, 2);
            var paid = Math.Round(face * discount, 2);
            var credit = count + (t.BonusCount ?? 0);
            CountCalcText.Text = BoundUnitPrice > 0
                ? $"充值金额 ¥{face:F2}（{count} 次 × ¥{BoundUnitPrice:F2}）　实收 ¥{paid:F2}　实充 {credit} 次（含赠送 {t.BonusCount ?? 0} 次）"
                : $"实充 {credit} 次（含赠送 {t.BonusCount ?? 0} 次）　绑定服务未配置会员价";
        }
        else
        {
            var amount = (decimal)AmountBox.Value;
            var paid = Math.Round(amount * discount, 2);
            var credit = Math.Round(amount + (t.BonusAmount ?? 0m), 2);
            StoredCalcText.Text = $"实收 ¥{paid:F2}（× {discount * 10m:0.#} 折）　实充 ¥{credit:F2}（含赠送 ¥{t.BonusAmount ?? 0m:F2}，= 卡内余额）";
        }
        RefreshReferralPreview();
    }

    /// <summary>当前开卡实收/充值基数（与 Save 写入的 InitialBalance 一致），用于推荐奖励预估。</summary>
    private decimal CurrentInitialPaid()
    {
        var t = SelectedType;
        if (t is null) return 0m;
        if (IsCountBased)
        {
            var face = Math.Round((int)CountBox.Value * BoundUnitPrice, 2);
            return Math.Round(face * t.Discount, 2);
        }
        return (decimal)AmountBox.Value;
    }

    /// <summary>按当前租户推荐规则，预估这次开卡将给顾客/员工推荐人产生的奖励。纯展示，后端最终核算。</summary>
    private void RefreshReferralPreview()
    {
        if (_referral is null || (!_referrerMemberId.HasValue && !_referrerStaffId.HasValue))
        {
            ReferralPreviewBox.Visibility = Visibility.Collapsed;
            return;
        }
        var paid = CurrentInitialPaid();
        var lines = new List<string>();
        if (_referrerMemberId.HasValue)
        {
            var (amt, desc) = _referral.CustomerReferralMode switch
            {
                "PercentPerRecharge" => (Math.Round(paid * _referral.CustomerRewardPercent / 100m, 2), $"充值返佣 {_referral.CustomerRewardPercent:0.#}%"),
                "FixedPerCard" => (_referral.CustomerFixedReward, "固定推荐费 / 张"),
                _ => (0m, "")
            };
            lines.Add(amt > 0
                ? $"顾客推荐人：返佣 ¥{amt:F2}（{desc}）→ 进推荐顾客余额"
                : "顾客推荐人：暂无返佣（推荐规则未开启或额度为 0）");
        }
        if (_referrerStaffId.HasValue)
        {
            var (amt, desc) = _referral.StaffReferralMode switch
            {
                "FixedPerCard" => (_referral.StaffReferralFixedAmount, "固定提成 / 张"),
                "PercentOfOpenCard" => (Math.Round(paid * _referral.StaffReferralPercent / 100m, 2), $"开卡实收 {_referral.StaffReferralPercent:0.#}%"),
                _ => (0m, "")
            };
            lines.Add(amt > 0
                ? $"员工推荐人：提成 ¥{amt:F2}（{desc}）→ 计入该员工当月工资"
                : "员工推荐人：暂无提成（推荐规则未开启或额度为 0）");
        }
        ReferralPreviewText.Text = string.Join("\n", lines);
        ReferralPreviewBox.Visibility = Visibility.Visible;
    }

    // ---- 引荐人：可输入下拉，输入即时远程搜索、按人聚合、选中即用、清空即取消 ----
    // 对齐 BS el-select filterable + remote + clearable。选项 = 聚合的"人"，取其首张可用卡 Id 作 ReferredByMemberId。

    /// <summary>下拉项：展示用 Label + 该人首张可用卡 Id。</summary>
    private sealed record ReferrerOption(string Label, long? MemberId);

    private System.Windows.Threading.DispatcherTimer? _refTimer;

    private void ReferrerBox_KeyUp(object sender, KeyEventArgs e)
    {
        // 方向/确认/Tab/Esc 等导航键交给下拉自身，不触发搜索
        if (e.Key is Key.Up or Key.Down or Key.Enter or Key.Tab or Key.Escape or Key.LeftShift or Key.RightShift) return;

        var text = ReferrerBox.Text?.Trim() ?? "";
        if (text.Length == 0)
        {
            // 清空 = 取消引荐人（对齐 BS clearable）
            _referrerMemberId = null;
            ReferrerSelLabel.Text = "无";
            ReferrerBox.ItemsSource = null;
            ReferrerBox.IsDropDownOpen = false;
            RefreshReferralPreview();
            return;
        }

        // 防抖 300ms 后远程搜索（对齐 BS remote-method）
        _refTimer ??= new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _refTimer.Tick -= RefTimer_Tick;
        _refTimer.Tick += RefTimer_Tick;
        _refTimer.Stop();
        _refTimer.Start();
    }

    private void RefTimer_Tick(object? sender, EventArgs e)
    {
        _refTimer?.Stop();
        _ = SearchReferrerAsync(ReferrerBox.Text?.Trim() ?? "");
    }

    private async System.Threading.Tasks.Task SearchReferrerAsync(string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) { ReferrerBox.ItemsSource = null; return; }
        try
        {
            var r = await _api.GetMemberGroupedAsync(page: 1, pageSize: 10, keyword: keyword, storeId: _storeId, includeClosed: false);
            var opts = r.Items.Select(g => new ReferrerOption(
                $"{g.Phone} · {g.PrimaryName ?? "未填"} · {g.CardCount} 张卡",
                (g.Cards.FirstOrDefault(c => c.IsActive) ?? g.Cards.FirstOrDefault())?.Id)).ToList();
            ReferrerBox.ItemsSource = opts;
            ReferrerBox.IsDropDownOpen = opts.Count > 0;
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    private void StaffReferrer_Picked(object sender, SelectionChangedEventArgs e)
    {
        _referrerStaffId = (StaffReferrerBox.SelectedItem as StaffOption)?.Id;
        RefreshReferralPreview();
    }

    private void Referrer_Picked(object sender, SelectionChangedEventArgs e)
    {
        // 仅当用户真正选中一项时更新；换数据源导致的清空不影响已选
        if (ReferrerBox.SelectedItem is not ReferrerOption opt) return;
        _referrerMemberId = opt.MemberId;
        ReferrerSelLabel.Text = opt.Label;
        RefreshReferralPreview();
    }

    private string? Gender()
    {
        foreach (var c in GenderPanel.Children)
            if (c is RadioButton { IsChecked: true } rb && rb.Tag is string tag) return tag;
        return null;
    }

    private string PayMethod()
    {
        foreach (var c in PayPanel.Children)
            if (c is RadioButton { IsChecked: true } rb && rb.CommandParameter is string tag) return tag;
        return "Wechat";
    }

    public CreateMemberRequest? Built { get; private set; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CardNoBox.Text) || string.IsNullOrWhiteSpace(PhoneBox.Text))
        {
            Warn("卡号与手机号必填"); return;
        }
        var t = SelectedType;
        if (t is null) { Warn("请选择会员类型"); return; }

        decimal initialBalance;
        int count;
        if (IsCountBased)
        {
            count = (int)CountBox.Value;
            if (count < (t.MinPurchaseCount ?? 1)) { Warn($"购买次数不能低于 {t.MinPurchaseCount ?? 1} 次"); return; }
            var face = Math.Round(count * BoundUnitPrice, 2);
            initialBalance = Math.Round(face * t.Discount, 2); // 实收
        }
        else
        {
            initialBalance = (decimal)AmountBox.Value; // 充值面值
            count = 0;
            if (initialBalance < (t.MinRechargeAmount ?? 0m)) { Warn($"充值金额不能低于 ¥{t.MinRechargeAmount ?? 0m:F2}"); return; }
        }

        Built = new CreateMemberRequest(
            StoreId: _storeId,
            CardNo: CardNoBox.Text.Trim(),
            Phone: PhoneBox.Text.Trim(),
            Name: string.IsNullOrWhiteSpace(NameBox.Text) ? null : NameBox.Text.Trim(),
            Gender: Gender(),
            Birthday: BirthdayBox.SelectedDate,
            Discount: t.Discount,   // 折扣以模板为准（后端也会覆盖）
            InitialBalance: initialBalance,
            Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim(),
            ReferredByMemberId: _referrerMemberId,
            ReferredByStaffId: _referrerStaffId,
            MemberTypeId: t.Id,
            Count: count,
            PayMethod: PayMethod());

        DialogResult = true;
        Close();
    }

    private static void Warn(string msg) =>
        MessageBox.Show(msg, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
