using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Plans;
using MassageSaas.Shared.Settings;
using MassageSaas.Shared.Subscriptions;

namespace MassageSaas.Cs.ViewModels;

/// <summary>
/// 服务订阅：与 BS 端 SubscriptionView 功能一致——查看订阅状态、选套餐、选年限/渠道、
/// 发起支付并轮询支付单状态，支付成功后自动刷新到期时间。
/// </summary>
public partial class SubscriptionViewModel : ObservableObject, IDisposable
{
    private readonly IApiClient _api;
    private readonly DispatcherTimer _pollTimer;

    public SubscriptionViewModel(IApiClient api)
    {
        _api = api;
        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _pollTimer.Tick += async (_, _) => await CheckOnceAsync();
        _ = ReloadAsync();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private TenantSubscriptionStatusDto? status;

    public bool HasStatus => Status is not null;

    [ObservableProperty]
    private ObservableCollection<PlanDto> plans = new();

    [ObservableProperty]
    private bool isBusy;

    // 平台端维护的展示配置（说明 + 客服联系方式），本端只读获取
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoticeText))]
    [NotifyPropertyChangedFor(nameof(ContactText))]
    [NotifyPropertyChangedFor(nameof(HasContact))]
    private PlatformSubscriptionSettingDto? notice;

    public string NoticeText => Notice?.ExpiryNotice ?? string.Empty;

    public bool HasContact =>
        !string.IsNullOrWhiteSpace(Notice?.ContactPhone) || !string.IsNullOrWhiteSpace(Notice?.ContactWechat);

    public string ContactText =>
        HasContact
            ? $"支付问题或线下支付请联系客服电话：{Notice?.ContactPhone ?? "—"}，添加微信号：{Notice?.ContactWechat ?? "—"} 咨询帮助。"
            : string.Empty;

    // —— 购买表单 ——
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalAmount))]
    private PlanDto? selectedPlan;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalAmount))]
    private int years = 1;

    /// <summary>支付渠道：Wechat / Alipay（与后端 PaymentChannel 一致）。</summary>
    [ObservableProperty]
    private string channel = "Wechat";

    public decimal TotalAmount => SelectedPlan is null ? 0m : SelectedPlan.AnnualPrice * Years;

    public string StatusLabel => Status?.Status switch
    {
        "Active" => "活跃",
        "Trial" => "试用中",
        "Expired" => "已过期",
        "Disabled" => "已停用",
        _ => Status?.Status ?? "—"
    };

    // —— 支付弹窗 ——
    [ObservableProperty]
    private bool isPayDialogOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PayStatusLabel))]
    [NotifyPropertyChangedFor(nameof(IsPayPending))]
    [NotifyPropertyChangedFor(nameof(IsPaySucceeded))]
    [NotifyPropertyChangedFor(nameof(IsPayFailed))]
    private SubscriptionPaymentDto? payOrder;

    [ObservableProperty]
    private bool creating;

    [ObservableProperty]
    private bool polling;

    public string PayStatusLabel => PayOrder?.Status switch
    {
        "Pending" => "待支付",
        "Paid" => "已支付",
        "Failed" => "已失败",
        "Refunded" => "已退款",
        _ => PayOrder?.Status ?? "—"
    };

    public bool IsPayPending => PayOrder?.Status == "Pending";
    public bool IsPaySucceeded => PayOrder?.Status == "Paid";
    public bool IsPayFailed => PayOrder?.Status is "Failed" or "Refunded";

    [RelayCommand]
    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            Status = await _api.GetMySubscriptionAsync();
            Notice = await _api.GetPlatformSubscriptionSettingAsync();
            var ps = await _api.GetPlansAsync(false);
            Plans = new ObservableCollection<PlanDto>(ps);
            // 默认选当前套餐；没有就选第一个
            if (SelectedPlan is null)
                SelectedPlan = ps.FirstOrDefault(p => p.Id == Status?.CurrentPlanId) ?? ps.FirstOrDefault();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void SelectPlan(PlanDto? plan)
    {
        if (plan is not null) SelectedPlan = plan;
    }

    [RelayCommand]
    private async Task CreatePaymentAsync()
    {
        if (Status is null || SelectedPlan is null) return;
        var yrs = Math.Clamp(Years, 1, 10);
        Creating = true;
        IsPayDialogOpen = true;
        PayOrder = null;
        try
        {
            PayOrder = await _api.CreateSubscriptionPaymentAsync(new CreateSubscriptionPaymentRequest(
                Status.TenantId, SelectedPlan.Id, yrs, Channel));
            _pollTimer.Start();
        }
        catch (Exception ex)
        {
            IsPayDialogOpen = false;
            ErrorReporter.Show(ex);
        }
        finally { Creating = false; }
    }

    [RelayCommand]
    private async Task CheckOnceAsync()
    {
        if (PayOrder is null) return;
        Polling = true;
        try
        {
            var cur = await _api.GetSubscriptionPaymentStatusAsync(PayOrder.OrderNo);
            PayOrder = PayOrder with { Status = cur.Status };
            if (cur.Status == "Paid")
            {
                _pollTimer.Stop();
                await ReloadAsync();
            }
            else if (cur.Status is "Failed" or "Refunded")
            {
                _pollTimer.Stop();
            }
        }
        catch { /* 轮询失败忽略，下一次 tick 再试 */ }
        finally { Polling = false; }
    }

    [RelayCommand]
    private void ClosePayDialog()
    {
        _pollTimer.Stop();
        IsPayDialogOpen = false;
    }

    public void Dispose() => _pollTimer.Stop();
}
