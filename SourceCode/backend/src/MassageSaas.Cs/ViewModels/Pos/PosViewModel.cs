using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Cs.Services.Devices;
using MassageSaas.Shared.Members;
using MassageSaas.Shared.Orders;
using MassageSaas.Shared.Rooms;
using MassageSaas.Shared.Services;
using MassageSaas.Shared.Staff;
using MassageSaas.Shared.Vouchers;

namespace MassageSaas.Cs.ViewModels.Pos;

public partial class PosViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;
    private readonly ISpeechAnnouncer _speech;
    private readonly IReceiptPrinter _printer;
    private readonly ICustomerDisplay _display;

    public PosViewModel(
        IApiClient api,
        AppContextService context,
        ISpeechAnnouncer speech,
        IReceiptPrinter printer,
        ICustomerDisplay display)
    {
        _api = api;
        _context = context;
        _speech = speech;
        _printer = printer;
        _display = display;
        _ = LoadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<ServiceItemDto> services = new();

    [ObservableProperty]
    private ObservableCollection<ServiceItemDto> filteredServices = new();

    [ObservableProperty]
    private ObservableCollection<StaffDto> technicians = new();

    [ObservableProperty]
    private ObservableCollection<RoomDto> rooms = new();

    [ObservableProperty]
    private ObservableCollection<CartItemViewModel> cart = new();

    [ObservableProperty]
    private string serviceFilter = string.Empty;

    [ObservableProperty]
    private string memberKeyword = string.Empty;

    [ObservableProperty]
    private MemberDto? activeMember;

    [ObservableProperty]
    private string payMethod = "Cash";

    [ObservableProperty]
    private decimal cashReceived;

    [ObservableProperty]
    private string? remark;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>收银员录入的券码（应用前的输入态）。</summary>
    [ObservableProperty]
    private string voucherCode = string.Empty;

    /// <summary>已应用的券（仅本地预览，doCheckout 才真正核销落库）。</summary>
    [ObservableProperty]
    private VoucherDto? appliedVoucher;

    [ObservableProperty]
    private bool voucherApplying;

    public decimal Total => Cart.Sum(c => c.LineTotal);

    public decimal MemberDiscount =>
        ActiveMember is { Discount: < 1 } m ? Math.Round(Total * (1 - m.Discount), 2) : 0m;

    /// <summary>
    /// 券抵扣：折扣率优先（与后端 OrdersController.Checkout 行为一致），不会超过订单合计。
    /// </summary>
    public decimal VoucherDiscount
    {
        get
        {
            if (AppliedVoucher is null || Total <= 0) return 0m;
            var raw = AppliedVoucher.DiscountPercent is > 0 and < 1
                ? Math.Round(Total * (1m - AppliedVoucher.DiscountPercent.Value), 2)
                : AppliedVoucher.FaceValue;
            return Math.Min(raw, Total);
        }
    }

    public decimal Payable => Math.Max(0, Total - MemberDiscount - VoucherDiscount);

    public decimal Change => PayMethod == "Cash" && CashReceived > Payable ? CashReceived - Payable : 0m;

    partial void OnPayMethodChanged(string value)
    {
        if (value == "Cash") CashReceived = Payable;
        OnPropertyChanged(nameof(Change));
    }

    partial void OnCashReceivedChanged(decimal value) => OnPropertyChanged(nameof(Change));

    partial void OnActiveMemberChanged(MemberDto? value)
    {
        // 切换会员后重算单价
        foreach (var c in Cart)
        {
            var svc = Services.FirstOrDefault(s => s.Id == c.ServiceId);
            if (svc is not null) c.UnitPrice = value is null ? svc.Price : svc.MemberPrice;
        }
        Recompute();
    }

    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            Services = new ObservableCollection<ServiceItemDto>(await _api.GetServicesAsync(false));
            ApplyFilter();
            var techs = await _api.GetStaffAsync(role: "Technician", pageSize: 200, storeId: _context.ActiveStoreId);
            Technicians = new ObservableCollection<StaffDto>(techs.Items);
            if (_context.ActiveStoreId is long sid)
            {
                try { Rooms = new ObservableCollection<RoomDto>(await _api.GetRoomsAsync(sid)); }
                catch { /* 房间领域 P2 后端可能尚未迁移 */ }
            }
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    partial void OnServiceFilterChanged(string value) => ApplyFilter();

    /// <summary>
    /// 快速开单：按搜索框里的字符匹配第一个服务并加入。给纯小键盘流程用：
    /// 输入服务编码 + Enter → 立刻添加；CartItem 默认指派第一个在岗技师（如有）。
    /// </summary>
    [RelayCommand]
    private void QuickAddFirst()
    {
        var first = FilteredServices.FirstOrDefault();
        if (first is null)
        {
            _speech.SayAsync("没有匹配的服务");
            return;
        }
        AddService(first);
        ServiceFilter = string.Empty;
        _speech.SayAsync($"已加入 {first.Name}");
    }

    private void ApplyFilter()
    {
        var k = (ServiceFilter ?? string.Empty).Trim().ToLowerInvariant();
        FilteredServices = string.IsNullOrEmpty(k)
            ? new ObservableCollection<ServiceItemDto>(Services)
            : new ObservableCollection<ServiceItemDto>(Services.Where(s =>
                s.Code.ToLowerInvariant().Contains(k) || s.Name.ToLowerInvariant().Contains(k)));
    }

    [RelayCommand]
    private async Task LookupMemberAsync()
    {
        var k = (MemberKeyword ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(k)) return;
        try
        {
            var page = await _api.GetMembersAsync(keyword: k, pageSize: 5, storeId: _context.ActiveStoreId);
            if (page.Items.Count == 0)
            {
                MessageBox.Show("未找到会员", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            ActiveMember = page.Items[0];
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>磁条会员卡刷卡：用卡号自动调出会员。由 MainViewModel 在收银台界面时转发。</summary>
    public void ApplyCardSwipe(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber)) return;
        MemberKeyword = cardNumber.Trim();
        if (LookupMemberCommand.CanExecute(null)) LookupMemberCommand.Execute(null);
    }

    /// <summary>测试打印：发一张样张并踢钱箱，现场调试打印机连线/编码/切纸用。</summary>
    [RelayCommand]
    private void TestPrint()
    {
        var result = _printer.SelfTest(_context.ActiveStore?.Name ?? "测试小票");
        _speech.SayAsync(result.Success ? "测试打印已发送" : "测试打印失败");
        MessageBox.Show(result.Message, result.Success ? "测试打印" : "测试打印失败",
            MessageBoxButton.OK,
            result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    [RelayCommand]
    private void ClearMember()
    {
        ActiveMember = null;
        MemberKeyword = string.Empty;
    }

    [RelayCommand]
    private void AddService(ServiceItemDto? svc)
    {
        if (svc is null) return;
        var unit = ActiveMember is null ? svc.Price : svc.MemberPrice;
        var defaultTech = Technicians.FirstOrDefault();
        Cart.Add(new CartItemViewModel
        {
            ServiceId = svc.Id,
            ServiceName = svc.Name,
            DurationMinutes = svc.DurationMinutes,
            UnitPrice = unit,
            Technician = defaultTech,
            Quantity = 1
        });
        Cart.Last().PropertyChanged += (_, __) => Recompute();
        Recompute();
    }

    [RelayCommand]
    private void RemoveCartItem(CartItemViewModel? item)
    {
        if (item is null) return;
        Cart.Remove(item);
        Recompute();
    }

    [RelayCommand]
    private void Clear()
    {
        if (Cart.Count == 0 && ActiveMember is null && AppliedVoucher is null) return;
        if (MessageBox.Show("确认清空当前订单？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
        Cart.Clear();
        ActiveMember = null;
        MemberKeyword = string.Empty;
        Remark = null;
        AppliedVoucher = null;
        VoucherCode = string.Empty;
        Recompute();
    }

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        if (Cart.Count == 0)
        {
            MessageBox.Show("购物车为空", "提示");
            return;
        }
        if (Cart.Any(c => c.Technician is null))
        {
            MessageBox.Show("请为所有项目指派技师", "提示");
            return;
        }
        if (_context.ActiveStoreId is null)
        {
            MessageBox.Show("未选择门店", "提示");
            return;
        }
        if (PayMethod == "MemberCard" && (ActiveMember is null || ActiveMember.Balance < Payable))
        {
            MessageBox.Show("会员卡余额不足或未关联会员", "提示");
            return;
        }
        if (PayMethod == "Cash" && CashReceived < Payable)
        {
            MessageBox.Show("实收金额不足", "提示");
            return;
        }
        // 次卡只能在购物车含其绑定服务时才允许结算；否则等同后端 PunchCardMismatch 提示
        if (ActiveMember is { MemberTypeKind: "CountBased" } pc)
        {
            var ok = pc.ServiceItemId.HasValue && Cart.Any(c => c.ServiceId == pc.ServiceItemId.Value);
            if (!ok)
            {
                var svcName = pc.ServiceItemName ?? "（未绑定服务）";
                MessageBox.Show($"次卡 {pc.CardNo} 绑定的服务「{svcName}」不在本单内，请添加该服务或改用其它会员卡。",
                    "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                _speech.SayAsync($"次卡未含{svcName}，无法结算");
                return;
            }
        }

        IsBusy = true;
        try
        {
            var created = await _api.CreateOrderAsync(new CreateOrderRequest(
                StoreId: _context.ActiveStoreId.Value,
                MemberId: ActiveMember?.Id,
                // TODO(轮钟/点钟): CS POS 尚未接入 queue/call-next 与 radio UI；
                // 不传 AssignmentSource 时后端 ParseSource 兜底为 Designation，
                // 提成自动按"仅点钟"或"通配"规则计算。BS POS 已支持完整切换，
                // 待 CS 端补 radio + 叫下一钟按钮后再透传 AssignmentSource。
                Items: Cart.Select(c => new OrderItemInputDto(
                    ServiceId: c.ServiceId,
                    TechnicianId: c.Technician!.Id,
                    Quantity: c.Quantity,
                    RoomId: c.Room?.Id)).ToList(),
                Remark: null));

            // 订单已落库；如果先前应用了券，这里 redeem 把券挂上去再 checkout。
            // redeem 失败就中断，订单留在 Pending 让收银员决定移除券或在订单流水里继续处理。
            if (AppliedVoucher is not null)
            {
                try
                {
                    await _api.RedeemVoucherAsync(new VoucherRedeemRequest(AppliedVoucher.Code, created.Id));
                }
                catch (Exception ex)
                {
                    ErrorReporter.Show(ex);
                    _speech.SayAsync("优惠券核销失败，订单已创建但未结账");
                    return;
                }
            }

            var checkedOut = await _api.CheckoutAsync(created.Id, new CheckoutRequest(
                PayMethod: PayMethod,
                PaidAmount: PayMethod == "Cash" ? CashReceived : null,
                DiscountAmount: 0m,
                Remark: Remark));

            // 结算即评价：弹出快速评价窗（默认满意），收银员可逐项调整或跳过
            if (checkedOut.Items.Count > 0)
            {
                var reviewDlg = new Views.CheckoutReviewWindow(_api, checkedOut);
                reviewDlg.ShowDialog();
            }

            ShowReceipt(checkedOut);
            Cart.Clear();
            ActiveMember = null;
            MemberKeyword = string.Empty;
            Remark = null;
            CashReceived = 0;
            AppliedVoucher = null;
            VoucherCode = string.Empty;
            Recompute();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    private void ShowReceipt(OrderDto o)
    {
        var change = PayMethod == "Cash" && CashReceived > o.PaidAmount ? CashReceived - o.PaidAmount : 0m;
        // 合计金额优先用面值（含次卡），缺失时退回 Total（兼容旧订单）
        var headlineTotal = o.ListTotal > 0 ? o.ListTotal : o.Total;
        var punchCount = o.PunchCardUsedCount;

        // 语音播报：盲人收银员/店长依赖此朗读关键金额
        var spoken = $"结账成功，应收 {YuanToReadable(o.PaidAmount)}";
        if (punchCount > 0) spoken += $"，次卡消费 {punchCount} 次";
        if (change > 0) spoken += $"，找零 {YuanToReadable(change)}";
        _speech.SayAsync(spoken);

        // 外设：打印小票、现金结账踢钱箱、客显展示实收金额
        _printer.Print(new ReceiptDocument(
            StoreName: _context.ActiveStore?.Name ?? string.Empty,
            OrderNo: o.OrderNo,
            PrintedAt: DateTime.Now,
            Items: o.Items
                .Select(i => new ReceiptLine(
                    i.ServiceName,
                    i.Quantity,
                    i.TechnicianName ?? "—",
                    ItemListAmount(i),
                    PaidViaPunchCard: i.MemberPackageId.HasValue))
                .ToList(),
            Total: headlineTotal,
            Discount: o.DiscountAmount,
            Paid: o.PaidAmount,
            Change: change,
            PayMethod: o.PayMethod,
            PunchCardUsedCount: punchCount));
        if (PayMethod == "Cash") _printer.OpenCashDrawer();
        _display.ShowAmount("实收", o.PaidAmount);

        var lines = new List<string>
        {
            $"订单：{o.OrderNo}",
            $"合计：¥{headlineTotal:F2}    实收：¥{o.PaidAmount:F2}（{o.PayMethod}）"
        };
        if (punchCount > 0) lines.Add($"消费次数：{punchCount} 次（次卡核销）");
        if (change > 0) lines.Add($"找零：¥{change:F2}");
        if (o.DiscountAmount > 0) lines.Add($"优惠：¥{o.DiscountAmount:F2}");
        lines.Add(string.Empty);
        foreach (var i in o.Items)
        {
            var tag = i.MemberPackageId.HasValue ? " [次卡]" : "";
            lines.Add($"· {i.ServiceName} × {i.Quantity}次  技师 {i.TechnicianName} ¥{ItemListAmount(i):F2}{tag}");
        }
        MessageBox.Show(string.Join(Environment.NewLine, lines), "结账成功",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>取明细的面值小计：优先 ListAmount，否则按 ListUnitPrice 算，再不济退回 ItemTotal。</summary>
    private static decimal ItemListAmount(OrderItemDto i)
    {
        if (i.ListAmount > 0) return i.ListAmount;
        if (i.ListUnitPrice > 0) return Math.Round(i.ListUnitPrice * i.Quantity, 2);
        return i.ItemTotal;
    }

    /// <summary>朗读金额："328 元 5 角"，避免读屏读"328.50"成"三百二十八点五零"。</summary>
    private static string YuanToReadable(decimal amount)
    {
        var yuan = (long)Math.Floor(amount);
        var jiao = (int)Math.Round((amount - yuan) * 10m);
        return jiao == 0 ? $"{yuan} 元" : $"{yuan} 元 {jiao} 角";
    }

    private void Recompute()
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(MemberDiscount));
        OnPropertyChanged(nameof(VoucherDiscount));
        OnPropertyChanged(nameof(Payable));
        OnPropertyChanged(nameof(Change));
        if (PayMethod == "Cash" && CashReceived < Payable) CashReceived = Payable;
        _display.ShowAmount("应付", Payable);
    }

    partial void OnAppliedVoucherChanged(VoucherDto? value) => Recompute();

    /// <summary>
    /// 录入券码 → 拉详情 → 本地校验状态/有效期/最低额 → 暂存。CheckoutAsync 创建订单后才真正 redeem。
    /// </summary>
    [RelayCommand]
    private async Task ApplyVoucherAsync()
    {
        var code = (VoucherCode ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(code))
        {
            MessageBox.Show("请填写券码", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (Cart.Count == 0)
        {
            MessageBox.Show("请先添加订单项", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        VoucherApplying = true;
        try
        {
            var v = await _api.GetVoucherByCodeAsync(code);
            if (!string.Equals(v.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                var msg = v.Status switch
                {
                    "Redeemed" => "该券已被核销",
                    "Expired" => "该券已过期",
                    "Cancelled" => "该券已作废",
                    _ => $"该券状态不可用：{v.Status}"
                };
                MessageBox.Show(msg, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                _speech.SayAsync(msg);
                return;
            }
            var now = DateTime.UtcNow;
            if (v.ValidFrom.HasValue && now < v.ValidFrom.Value)
            {
                MessageBox.Show("该券尚未生效", "提示");
                return;
            }
            if (v.ExpiresAt.HasValue && now > v.ExpiresAt.Value)
            {
                MessageBox.Show("该券已过期", "提示");
                return;
            }
            if (v.MinOrderAmount > 0 && Total < v.MinOrderAmount)
            {
                MessageBox.Show($"订单不足 ¥{v.MinOrderAmount:F2}，未达到券使用门槛", "提示");
                return;
            }
            AppliedVoucher = v;
            _speech.SayAsync($"已应用券，抵扣 {YuanToReadable(VoucherDiscount)}");
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { VoucherApplying = false; }
    }

    [RelayCommand]
    private void RemoveVoucher()
    {
        AppliedVoucher = null;
        VoucherCode = string.Empty;
    }
}
