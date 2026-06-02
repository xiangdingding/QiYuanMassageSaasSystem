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

    /// <summary>本店所有计时 session（含历史，按 Status 过滤 Open）。</summary>
    [ObservableProperty]
    private ObservableCollection<TimedRoomSessionDto> timedSessions = new();

    /// <summary>「计时房费」区卡片：每间计时房一张，含当前 Open session 与是否已入车。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTimedRooms))]
    private ObservableCollection<TimedRoomCardViewModel> timedRoomCards = new();

    public bool HasTimedRooms => TimedRoomCards.Count > 0;

    [ObservableProperty]
    private ObservableCollection<CartItemViewModel> cart = new();

    [ObservableProperty]
    private string serviceFilter = string.Empty;

    [ObservableProperty]
    private string memberKeyword = string.Empty;

    /// <summary>收银模式：member=会员收银（按会员价、可关联会员卡合并结算）；walkin=散客收银（标准价、无会员）。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMemberMode))]
    [NotifyPropertyChangedFor(nameof(IsWalkinMode))]
    private string mode = "member";

    /// <summary>按手机号查出的会员名下所有卡（充值卡 + 次卡）。可勾选多张合并结算。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMemberCards))]
    [NotifyPropertyChangedFor(nameof(MultipleCards))]
    [NotifyPropertyChangedFor(nameof(PrimaryPhone))]
    private ObservableCollection<PosMemberCardViewModel> memberCards = new();

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

    public bool IsMemberMode => Mode == "member";
    public bool IsWalkinMode => Mode == "walkin";
    public bool HasMemberCards => MemberCards.Count > 0;
    public bool MultipleCards => MemberCards.Count > 1;
    public string PrimaryPhone => MemberCards.FirstOrDefault()?.Member.Phone ?? string.Empty;

    public IEnumerable<PosMemberCardViewModel> SelectedCards => MemberCards.Where(c => c.IsSelected);
    /// <summary>主卡：第一张勾选的卡，作 order.MemberId；其余为 SecondaryMemberIds。</summary>
    public MemberDto? PrimaryMember => SelectedCards.FirstOrDefault()?.Member;
    /// <summary>兼容旧绑定/逻辑：当前会员即主卡。</summary>
    public MemberDto? ActiveMember => PrimaryMember;
    public bool HasMember => PrimaryMember is not null;
    public int SelectedCount => SelectedCards.Count();
    public decimal SelectedBalance => SelectedCards.Sum(c => c.Member.Balance);

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

    partial void OnModeChanged(string value)
    {
        // 切到散客：解除会员关联、回到标准价；切回会员：仅重算状态（需重新查询会员）
        if (value == "walkin") ClearMemberInternal();
        else RecomputeMemberState();
    }

    partial void OnMemberKeywordChanged(string value)
    {
        // 输入框被清空时，已查出的会员卡一并清掉，避免"框空了还挂着上个会员"的歧义
        if (string.IsNullOrWhiteSpace(value) && MemberCards.Count > 0) ClearMemberInternal();
    }

    private bool _suppressCardEvents;

    /// <summary>勾选/取消某张会员卡时：拦截不可选卡的误勾，然后重算主卡/余额/单价。</summary>
    private void OnCardPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_suppressCardEvents || sender is not PosMemberCardViewModel card) return;
        if (e.PropertyName != nameof(PosMemberCardViewModel.IsSelected)) return;
        if (card.IsSelected && !card.CanSelect)
        {
            _suppressCardEvents = true;
            card.IsSelected = false;
            _suppressCardEvents = false;
            var msg = card.ServiceItemName is not null
                ? $"次卡绑定的「{card.ServiceItemName}」不在购物车里，不能选用"
                : "该次卡未绑定服务项目，不能用于结算";
            MessageBox.Show(msg, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        RecomputeMemberState();
    }

    /// <summary>会员选择变动后：重算主卡/合计余额、按"是否关联会员"重置单价、刷新支付方式可用性。</summary>
    private void RecomputeMemberState()
    {
        OnPropertyChanged(nameof(PrimaryMember));
        OnPropertyChanged(nameof(ActiveMember));
        OnPropertyChanged(nameof(HasMember));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedBalance));
        // 价格按"是否已关联会员"取会员价/标准价（与原行为一致）
        foreach (var c in Cart)
        {
            var svc = Services.FirstOrDefault(s => s.Id == c.ServiceId);
            if (svc is not null) c.UnitPrice = HasMember ? svc.MemberPrice : svc.Price;
        }
        // 没有会员时，会员卡支付不可用，回退现金
        if (!HasMember && PayMethod == "MemberCard") PayMethod = "Cash";
        Recompute();
    }

    /// <summary>按购物车内容刷新各卡可选性：次卡仅当其绑定服务在购物车里才可选；
    /// 已勾选却变得不可选的卡自动取消（与 BS 一致，避免落 400 PunchCardMismatch）。</summary>
    private void RefreshCardEligibility()
    {
        if (MemberCards.Count == 0) return;
        var dropped = false;
        foreach (var card in MemberCards)
        {
            var eligible = !card.IsCountBased
                || (card.ServiceItemId is long sid && Cart.Any(c => c.ServiceId == sid));
            card.IsEligible = eligible;
            if (!eligible && card.IsSelected)
            {
                _suppressCardEvents = true;
                card.IsSelected = false;
                _suppressCardEvents = false;
                dropped = true;
            }
        }
        if (dropped) _speech.SayAsync("部分次卡因购物车无匹配服务已取消");
        RecomputeMemberState();
    }

    /// <summary>替换会员卡列表并挂上勾选监听（先退订旧的，避免泄漏）。</summary>
    private void SetMemberCards(IEnumerable<MemberDto> cards)
    {
        foreach (var c in MemberCards) c.PropertyChanged -= OnCardPropertyChanged;
        var coll = new ObservableCollection<PosMemberCardViewModel>();
        foreach (var m in cards)
        {
            var vm = new PosMemberCardViewModel(m);
            vm.PropertyChanged += OnCardPropertyChanged;
            coll.Add(vm);
        }
        MemberCards = coll;
    }

    private void ClearMemberInternal()
    {
        foreach (var c in MemberCards) c.PropertyChanged -= OnCardPropertyChanged;
        MemberCards = new();
        MemberKeyword = string.Empty;
        RecomputeMemberState();
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
                try { TimedSessions = new ObservableCollection<TimedRoomSessionDto>(await _api.GetTimedRoomSessionsAsync(sid)); }
                catch { /* 计时房后端可能尚未迁移 */ }
            }
            RebuildTimedRoomCards();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    /// <summary>按 Rooms + TimedSessions + Cart 重建「计时房费」卡片（计时房 = IsTimedRoom 且启用）。</summary>
    private void RebuildTimedRoomCards()
    {
        var cards = new ObservableCollection<TimedRoomCardViewModel>();
        foreach (var room in Rooms.Where(r => r.IsTimedRoom && r.IsActive))
        {
            var session = TimedSessions.FirstOrDefault(s => s.RoomId == room.Id && s.Status == "Open");
            var inCart = session is not null && Cart.Any(c => c.IsRoomCharge && c.SessionId == session.Id);
            cards.Add(new TimedRoomCardViewModel(room, session, inCart));
        }
        TimedRoomCards = cards;
    }

    /// <summary>刷新计时 session（开台/取消/结算后调用，避免拿过期分钟数）。</summary>
    private async Task ReloadTimedSessionsAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        try { TimedSessions = new ObservableCollection<TimedRoomSessionDto>(await _api.GetTimedRoomSessionsAsync(sid)); }
        catch { /* http 已弹错 */ }
        RebuildTimedRoomCards();
    }

    /// <summary>在收银台给一间计时房开台（弹窗选会员/散客/备注）。</summary>
    [RelayCommand]
    private async Task OpenStartTimingAsync(TimedRoomCardViewModel? card)
    {
        if (card is null) return;
        var dlg = new Views.StartTimingWindow(_api, card.Room, _context.ActiveStoreId)
        {
            Owner = Application.Current?.MainWindow
        };
        if (dlg.ShowDialog() == true)
        {
            await ReloadTimedSessionsAsync();
            _speech.SayAsync($"{card.RoomNo} 已开台计时");
        }
    }

    /// <summary>把进行中的计时 session 作为一行 roomCharge 加入购物车（金额按当前已计时分钟快照）。</summary>
    [RelayCommand]
    private void AddRoomCharge(TimedRoomCardViewModel? card)
    {
        if (card?.Session is null) return;
        if (Cart.Any(c => c.IsRoomCharge && c.SessionId == card.Session.Id))
        {
            _speech.SayAsync("该房间已在订单里");
            return;
        }
        var minutes = card.Session.ElapsedMinutes;
        var amount = Math.Round(minutes / 60m * card.Session.HourlyRateSnapshot, 2);
        var item = new CartItemViewModel
        {
            Kind = "roomCharge",
            ServiceName = $"{card.RoomNo} 计时房 {minutes} 分钟",
            SessionId = card.Session.Id,
            RoomId = card.Room.Id,
            RoomNo = card.RoomNo,
            ElapsedMinutes = minutes,
            HourlyRate = card.Session.HourlyRateSnapshot,
            BoundMemberId = card.Session.MemberId,
            BoundMemberName = card.Session.MemberName,
            UnitPrice = amount,
            Quantity = 1
        };
        Cart.Add(item);
        item.PropertyChanged += (_, __) => Recompute();
        Recompute();
        RebuildTimedRoomCards(); // 标记该房"已加入"
        if (IsRoomChargeMismatch(item))
            _speech.SayAsync($"{card.RoomNo} 开台会员与当前结算会员不一致，结算前请关联同一会员");
    }

    /// <summary>取消一段进行中的计时（误开台/客人临时不消费）：session 作废、不计费，并从购物车移除。</summary>
    [RelayCommand]
    private async Task CancelTimingAsync(TimedRoomCardViewModel? card)
    {
        if (card?.Session is null) return;
        if (MessageBox.Show(
                $"确认取消 {card.RoomNo} 的计时？已计 {card.Session.ElapsedMinutes} 分钟将作废、不计费。",
                "取消计时", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.CancelTimedRoomAsync(card.Session.Id);
            var inCart = Cart.FirstOrDefault(c => c.IsRoomCharge && c.SessionId == card.Session.Id);
            if (inCart is not null) { Cart.Remove(inCart); Recompute(); }
            await ReloadTimedSessionsAsync();
            _speech.SayAsync($"{card.RoomNo} 计时已取消");
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>计时房费"绑定会员 vs 结算会员"匹配：散客开台无约束；绑定会员开台必须当前已查询到同一手机号下的卡。</summary>
    private bool IsRoomChargeMismatch(CartItemViewModel item)
    {
        if (!item.IsRoomCharge || item.BoundMemberId is null) return false;
        if (PrimaryMember is null) return true;
        return !MemberCards.Any(c => c.Id == item.BoundMemberId);
    }

    private IEnumerable<CartItemViewModel> MismatchedRoomCharges =>
        Cart.Where(c => c.IsRoomCharge && IsRoomChargeMismatch(c));

    partial void OnServiceFilterChanged(string value) => ApplyFilter();

    /// <summary>清除服务搜索框（旁边的 ✕ 按钮 / Esc），恢复显示全部服务。</summary>
    [RelayCommand]
    private void ClearServiceFilter() => ServiceFilter = string.Empty;

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
            // 先用关键字（卡号/手机号）定位一张卡，再按其手机号把名下所有卡拉出来。
            // includeClosed=true 让已退/已关闭的历史卡也显示（UI 区分、不可勾选）。逻辑同 BS 端 lookupMember。
            var first = await _api.GetMembersAsync(
                keyword: k, pageSize: 5, storeId: _context.ActiveStoreId, includeClosed: true);
            if (first.Items.Count == 0)
            {
                ClearMemberInternal();
                MessageBox.Show("未找到会员", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var phone = first.Items[0].Phone;
            var all = await _api.GetMembersAsync(
                keyword: phone, pageSize: 100, storeId: _context.ActiveStoreId, includeClosed: true);
            SetMemberCards(all.Items.Where(m => m.Phone == phone));
            RefreshCardEligibility();
            // 默认勾选命中的那张；命中卡不可选（已关闭/无匹配）时退而选第一张可选卡
            var hit = MemberCards.FirstOrDefault(c => c.Id == first.Items[0].Id);
            var pick = hit is { CanSelect: true } ? hit : MemberCards.FirstOrDefault(c => c.CanSelect);
            if (pick is not null)
            {
                _suppressCardEvents = true;
                pick.IsSelected = true;
                _suppressCardEvents = false;
            }
            RecomputeMemberState();
            var activeCnt = MemberCards.Count(c => c.IsActive);
            _speech.SayAsync(activeCnt > 1
                ? $"已找到{activeCnt}张可用卡，可勾选合并结算"
                : "已关联会员");
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>磁条会员卡刷卡：切到会员模式并用卡号自动调出会员。由 MainViewModel 在收银台界面时转发。</summary>
    public void ApplyCardSwipe(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber)) return;
        Mode = "member";
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
    private void ClearMember() => ClearMemberInternal();

    [RelayCommand]
    private void AddService(ServiceItemDto? svc)
    {
        if (svc is null) return;
        var unit = HasMember ? svc.MemberPrice : svc.Price;
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
        // 购物车项变化可能让某些次卡变为可选/不可选
        RefreshCardEligibility();
    }

    [RelayCommand]
    private void RemoveCartItem(CartItemViewModel? item)
    {
        if (item is null) return;
        Cart.Remove(item);
        Recompute();
        RefreshCardEligibility();
        // 移除计时房费行后，对应计时房卡片恢复"加入订单"
        if (item.IsRoomCharge) RebuildTimedRoomCards();
    }

    [RelayCommand]
    private void Clear()
    {
        if (Cart.Count == 0 && !HasMemberCards && AppliedVoucher is null) return;
        if (MessageBox.Show("确认清空当前订单？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
        Cart.Clear();
        ClearMemberInternal();
        Remark = null;
        AppliedVoucher = null;
        VoucherCode = string.Empty;
        Recompute();
        RebuildTimedRoomCards();
    }

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        if (Cart.Count == 0)
        {
            MessageBox.Show("购物车为空", "提示");
            return;
        }
        // 仅服务项要求指派技师；计时房费行无技师
        if (Cart.Any(c => c.IsService && c.Technician is null))
        {
            MessageBox.Show("请为所有服务项目指派技师", "提示");
            return;
        }
        if (_context.ActiveStoreId is null)
        {
            MessageBox.Show("未选择门店", "提示");
            return;
        }
        // 计时房费绑定会员必须与当前结算会员一致，否则不允许结算（避免归账到错的会员）
        var mismatched = MismatchedRoomCharges.ToList();
        if (mismatched.Count > 0)
        {
            var desc = string.Join("、", mismatched.Select(r => $"{r.RoomNo}（开台 {r.BoundMemberName ?? "—"}）"));
            MessageBox.Show($"计时房费 {desc} 与当前结算会员不一致，请关联同一会员或将该房费从订单中移除。",
                "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            _speech.SayAsync("计时房费会员不一致，无法结算");
            return;
        }
        // 会员卡支付：用所有勾选卡的合计余额判断是否够付（合并结算）
        if (PayMethod == "MemberCard" && (!HasMember || SelectedBalance < Payable))
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
        foreach (var pc in SelectedCards.Where(c => c.IsCountBased).ToList())
        {
            var ok = pc.ServiceItemId is long sid && Cart.Any(c => c.ServiceId == sid);
            if (!ok)
            {
                var svcName = pc.ServiceItemName ?? "（未绑定服务）";
                MessageBox.Show($"次卡 {pc.CardNo} 绑定的服务「{svcName}」不在本单内，请添加该服务或取消勾选该卡。",
                    "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                _speech.SayAsync($"次卡未含{svcName}，无法结算");
                return;
            }
        }

        IsBusy = true;
        try
        {
            // 服务项走 Items；计时房费走 RoomSessionIds（Checkout 时后端会一并收尾这些 session 并入订单总额）
            var serviceLines = Cart.Where(c => c.IsService).ToList();
            var roomSessionIds = Cart.Where(c => c.IsRoomCharge).Select(c => c.SessionId).ToList();
            var created = await _api.CreateOrderAsync(new CreateOrderRequest(
                StoreId: _context.ActiveStoreId.Value,
                MemberId: PrimaryMember?.Id,
                // TODO(轮钟/点钟): CS POS 尚未接入 queue/call-next 与 radio UI；
                // 不传 AssignmentSource 时后端 ParseSource 兜底为 Designation，
                // 提成自动按"仅点钟"或"通配"规则计算。BS POS 已支持完整切换，
                // 待 CS 端补 radio + 叫下一钟按钮后再透传 AssignmentSource。
                Items: serviceLines.Select(c => new OrderItemInputDto(
                    ServiceId: c.ServiceId,
                    TechnicianId: c.Technician!.Id,
                    Quantity: c.Quantity,
                    RoomId: c.Room?.Id)).ToList(),
                Remark: null,
                RoomSessionIds: roomSessionIds.Count > 0 ? roomSessionIds : null));

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

            // 合并结算：主卡之外勾选的卡走 SecondaryMemberIds（仅 MemberCard 支付时后端会用）
            var secondary = SelectedCards.Skip(1).Select(c => c.Id).ToList();
            var checkedOut = await _api.CheckoutAsync(created.Id, new CheckoutRequest(
                PayMethod: PayMethod,
                PaidAmount: PayMethod == "Cash" ? CashReceived : null,
                DiscountAmount: 0m,
                Remark: Remark,
                SecondaryMemberIds: secondary.Count > 0 ? secondary : null));

            // 结算即评价：弹出快速评价窗（默认满意），收银员可逐项调整或跳过
            if (checkedOut.Items.Count > 0)
            {
                var reviewDlg = new Views.CheckoutReviewWindow(_api, checkedOut);
                reviewDlg.ShowDialog();
            }

            ShowReceipt(checkedOut);
            Cart.Clear();
            ClearMemberInternal();
            Remark = null;
            CashReceived = 0;
            AppliedVoucher = null;
            VoucherCode = string.Empty;
            Recompute();
            // 已结算的计时 session 已收尾消失，刷新计时房卡片
            await ReloadTimedSessionsAsync();
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
