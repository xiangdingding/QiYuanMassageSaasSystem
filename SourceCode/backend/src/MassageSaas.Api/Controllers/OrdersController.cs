using MassageSaas.Application.Abstractions;
using MassageSaas.Application.Commissions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Common;
using MassageSaas.Shared.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Policy = "ShopStaff")]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ApplicationDbContext db, ITenantContext tenantContext, ILogger<OrdersController> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderListItemDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] long? storeId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var pq = new PageQuery(page, pageSize, null);
        var q = _db.Orders.AsNoTracking().Include(o => o.Member).AsQueryable();

        if (storeId.HasValue) q = q.Where(o => o.StoreId == storeId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var s))
            q = q.Where(o => o.Status == s);
        if (from.HasValue) q = q.Where(o => o.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(o => o.CreatedAt < to.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pq.SafePage - 1) * pq.SafePageSize)
            .Take(pq.SafePageSize)
            .Select(o => new OrderListItemDto(
                o.Id, o.OrderNo, o.Total, o.PaidAmount,
                o.PayMethod.ToString(), o.Status.ToString(),
                // 项数把计时房费也算上一行，避免"纯房费单"显示 0 项造成误读
                o.Items.Count + o.RoomSessions.Count, o.CreatedAt, o.CompletedAt,
                o.Member != null ? o.Member.CardNo : null,
                o.Member != null ? o.Member.Phone : null))
            .ToListAsync(ct);

        return Ok(new PagedResult<OrderListItemDto>(items, total, pq.SafePage, pq.SafePageSize));
    }

    /// <summary>
    /// 按技师 + 业务日列出他在该日已完成的订单项，供"登记投诉"弹窗选择。
    /// 业务日按门店切日时间换算；只返回 Order.Status = Completed 的项。
    /// HasPendingComplaint 字段供 UI 把已存在未处理投诉的行禁选。
    /// </summary>
    [HttpGet("items/by-technician")]
    public async Task<ActionResult<IReadOnlyList<TechnicianServedItemDto>>> ItemsByTechnician(
        [FromQuery] long storeId,
        [FromQuery] long technicianId,
        [FromQuery] DateTime date,
        CancellationToken ct = default)
    {
        var cutoff = await _db.Stores.AsNoTracking()
            .Where(s => s.Id == storeId)
            .Select(s => (int?)s.DayCloseCutoffMinutes)
            .FirstOrDefaultAsync(ct) ?? 0;
        var bizDate = DateOnly.FromDateTime(date);
        var (start, end) = MassageSaas.Domain.Common.BusinessDayCalculator.RangeOf(bizDate, cutoff);

        var rows = await _db.OrderItems.AsNoTracking()
            .Where(oi => oi.Order.StoreId == storeId
                         && oi.TechnicianId == technicianId
                         && oi.Order.Status == OrderStatus.Completed
                         && oi.Order.CompletedAt >= start && oi.Order.CompletedAt < end)
            .OrderByDescending(oi => oi.Order.CompletedAt)
            .Select(oi => new TechnicianServedItemDto(
                oi.Id, oi.OrderId, oi.Order.OrderNo,
                oi.ServiceId, oi.ServiceName,
                oi.Order.CompletedAt, oi.ItemTotal,
                oi.Order.MemberId,
                oi.Order.Member != null ? oi.Order.Member.Name : null,
                oi.Order.Member != null ? oi.Order.Member.CardNo : null,
                _db.ServiceComplaints.Any(c => c.OrderItemId == oi.Id && c.Status == ComplaintStatus.Pending)))
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<OrderDto>> Get(long id, CancellationToken ct)
    {
        var o = await LoadOrderAsync(id, ct);
        if (o is null) return NotFound();
        return Ok(ToDto(o));
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderRequest req, CancellationToken ct)
    {
        var items = req.Items ?? Array.Empty<OrderItemInputDto>();
        var roomSessionIds = (req.RoomSessionIds ?? Array.Empty<long>()).Distinct().ToList();
        if (items.Count == 0 && roomSessionIds.Count == 0)
            return BadRequest(new { code = "InvalidInput", message = "至少需要一个服务项或一笔计时房费" });

        var serviceIds = items.Select(i => i.ServiceId).Distinct().ToList();
        var techIds = items.Select(i => i.TechnicianId).Distinct().ToList();

        var services = serviceIds.Count == 0
            ? new Dictionary<long, ServiceItem>()
            : await _db.ServiceItems.AsNoTracking()
                .Where(s => serviceIds.Contains(s.Id) && s.IsActive)
                .ToDictionaryAsync(s => s.Id, ct);
        if (services.Count != serviceIds.Count)
            return BadRequest(new { code = "ServiceNotFound", message = "存在未启用或不存在的服务项" });

        if (techIds.Count > 0)
        {
            var techCount = await _db.Users.CountAsync(u =>
                techIds.Contains(u.Id) && u.Role == UserRole.Technician && u.IsActive, ct);
            if (techCount != techIds.Count)
                return BadRequest(new { code = "TechnicianNotFound", message = "存在不存在或已停用的技师" });
        }

        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == req.StoreId && s.IsActive, ct);
        if (store is null) return BadRequest(new { code = "StoreNotFound", message = "门店不存在或已停用" });

        // 房间校验：仅校验"属于该门店且启用"。房间是纯属性，无独占语义——
        // 多个 OrderItem 可同时引用同一房间，由前台自行协调实际物理使用。
        var roomIds = items.Where(i => i.RoomId.HasValue).Select(i => i.RoomId!.Value).Distinct().ToList();
        Dictionary<long, Room> rooms = new();
        if (roomIds.Count > 0)
        {
            var rs = await _db.Rooms.AsNoTracking()
                .Where(r => roomIds.Contains(r.Id) && r.StoreId == req.StoreId && r.IsActive)
                .ToListAsync(ct);
            if (rs.Count != roomIds.Count)
                return BadRequest(new { code = "RoomInvalid", message = "存在不属于该门店或已停用的房间" });
            rooms = rs.ToDictionary(r => r.Id);
        }

        Member? member = null;
        if (req.MemberId.HasValue)
        {
            member = await _db.Members.FirstOrDefaultAsync(x => x.Id == req.MemberId.Value, ct);
            if (member is null) return BadRequest(new { code = "MemberNotFound", message = "会员不存在" });
        }

        // 校验并预约计时房 session：必须属于本店、Open、并且若开台绑定了会员则必须与当前结算人一致
        var sessions = new List<TimedRoomSession>();
        if (roomSessionIds.Count > 0)
        {
            sessions = await _db.TimedRoomSessions
                .Where(s => roomSessionIds.Contains(s.Id))
                .ToListAsync(ct);
            if (sessions.Count != roomSessionIds.Count)
                return BadRequest(new { code = "RoomSessionNotFound", message = "存在不存在的计时房记录" });
            foreach (var s in sessions)
            {
                if (s.StoreId != req.StoreId)
                    return BadRequest(new { code = "RoomSessionStoreMismatch", message = "计时房记录不属于该门店" });
                if (s.Status != TimedRoomSessionStatus.Open)
                    return Conflict(new { code = "RoomSessionNotOpen", message = "存在已结算/作废的计时房记录" });
                if (s.OrderId is not null)
                    return Conflict(new { code = "RoomSessionTaken", message = "计时房记录已挂在其它订单" });
                if (s.MemberId is long boundCardId)
                {
                    // 同手机号下的任意一张卡视为同一人；与前端校验口径一致
                    var samePerson = await IsSamePersonAsync(boundCardId, member?.Id, ct);
                    if (!samePerson)
                        return BadRequest(new { code = "RoomSessionMemberMismatch", message = "计时房开台会员与当前结算会员不一致" });
                }
            }
        }

        var order = new Order
        {
            StoreId = req.StoreId,
            MemberId = member?.Id,
            OrderNo = GenerateOrderNo(),
            CashierUserId = _tenantContext.UserId,
            Status = OrderStatus.Pending,
            PayMethod = PayMethod.Unpaid,
            StartedAt = DateTime.UtcNow,
            Remark = req.Remark
        };

        var packages = await LoadActivePackagesAsync(member?.Id, ct);
        decimal total = 0m;
        foreach (var input in items)
        {
            var svc = services[input.ServiceId];
            var qty = input.Quantity < 1 ? 1 : input.Quantity;

            var techLevel = await ResolveTechLevelAsync(input.TechnicianId, ct);
            var unit = ResolvePrice(svc, member, techLevel);
            decimal lineTotal = Math.Round(unit * qty, 2);
            // 面值始终保留：次卡核销也要在小票上展示服务原价 / 消费次数
            var listUnit = unit;

            var pkg = TryConsumePackage(packages, svc.Id, qty);
            if (pkg is not null) { unit = 0m; lineTotal = 0m; }
            total += lineTotal;

            Room? room = null;
            if (input.RoomId.HasValue) rooms.TryGetValue(input.RoomId.Value, out room);
            order.Items.Add(new OrderItem
            {
                ServiceId = svc.Id,
                ServiceName = svc.Name,
                TechnicianId = input.TechnicianId,
                DurationMinutes = svc.DurationMinutes,
                UnitPrice = unit,
                Quantity = qty,
                ItemTotal = lineTotal,
                ListUnitPrice = listUnit,
                CommissionAmount = 0m,
                RoomId = room?.Id,
                RoomNoSnapshot = room?.RoomNo,
                MemberPackageId = pkg?.Id,
                IsAddOn = false,
                AssignmentSource = ParseSource(input.AssignmentSource)
            });
        }
        // 计时房费先按当下分钟数预估并入 Total（结账时会按真实结算分钟数重算）
        foreach (var s in sessions)
        {
            var minutes = Math.Max(1, (int)Math.Ceiling((DateTime.UtcNow - s.StartedAt).TotalMinutes));
            total += Math.Round(minutes / 60m * s.HourlyRateSnapshot, 2, MidpointRounding.AwayFromZero);
        }
        order.Total = total;
        order.PaidAmount = 0m;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        if (sessions.Count > 0)
        {
            foreach (var s in sessions) s.OrderId = order.Id;
            await _db.SaveChangesAsync(ct);
        }

        var saved = await LoadOrderAsync(order.Id, ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, ToDto(saved!));
    }

    /// <summary>解析前端传入的 AssignmentSource 字符串：合法的 Rotation/Designation 直接用，
    /// 空/无效/Unknown 一律兜底为 Designation —— 沉默的老客户端做的是手动选人，本就是点钟，
    /// 这样指定率不会被假性轮钟稀释。</summary>
    private static AssignmentSource ParseSource(string? s) =>
        Enum.TryParse<AssignmentSource>(s, true, out var v) && v != AssignmentSource.Unknown
            ? v
            : AssignmentSource.Designation;

    /// <summary>同手机号下的任意一张卡视为同一人；用于计时房开台 cardId 与下单 cardId 的人维度比对。</summary>
    private async Task<bool> IsSamePersonAsync(long boundCardId, long? currentCardId, CancellationToken ct)
    {
        if (!currentCardId.HasValue) return false;
        if (boundCardId == currentCardId.Value) return true;
        var phones = await _db.Members.AsNoTracking()
            .Where(m => m.Id == boundCardId || m.Id == currentCardId.Value)
            .Select(m => m.Phone).Distinct().ToListAsync(ct);
        return phones.Count == 1;
    }

    /// <summary>
    /// 加钟：在未结账订单上追加 OrderItem（与转钟不同，不替换技师，而是同一单多做一段）。
    /// </summary>
    [HttpPost("{id:long}/items")]
    public async Task<ActionResult<OrderDto>> AddItems(long id, [FromBody] AddOrderItemsRequest req, CancellationToken ct)
    {
        if (req.Items is null || req.Items.Count == 0)
            return BadRequest(new { code = "InvalidInput", message = "加钟需提供至少一个服务项" });

        var order = await _db.Orders.Include(o => o.Items).Include(o => o.Member)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.InProgress)
            return Conflict(new { code = "InvalidState", message = "已结账或已取消订单不可加钟" });

        var svcIds = req.Items.Select(i => i.ServiceId).Distinct().ToList();
        var services = await _db.ServiceItems.AsNoTracking()
            .Where(s => svcIds.Contains(s.Id) && s.IsActive)
            .ToDictionaryAsync(s => s.Id, ct);
        if (services.Count != svcIds.Count)
            return BadRequest(new { code = "ServiceNotFound", message = "存在不存在或停用的服务" });

        var packages = await LoadActivePackagesAsync(order.MemberId, ct);
        foreach (var input in req.Items)
        {
            var svc = services[input.ServiceId];
            var qty = input.Quantity < 1 ? 1 : input.Quantity;
            var techLevel = await ResolveTechLevelAsync(input.TechnicianId, ct);
            var unit = ResolvePrice(svc, order.Member, techLevel);
            decimal lineTotal = Math.Round(unit * qty, 2);
            var listUnit = unit;
            var pkg = TryConsumePackage(packages, svc.Id, qty);
            if (pkg is not null) { unit = 0m; lineTotal = 0m; }

            order.Items.Add(new OrderItem
            {
                ServiceId = svc.Id,
                ServiceName = svc.Name,
                TechnicianId = input.TechnicianId,
                DurationMinutes = svc.DurationMinutes,
                UnitPrice = unit,
                Quantity = qty,
                ItemTotal = lineTotal,
                ListUnitPrice = listUnit,
                RoomId = input.RoomId,
                MemberPackageId = pkg?.Id,
                IsAddOn = true,
                AssignmentSource = ParseSource(input.AssignmentSource)
            });
            order.Total += lineTotal;
        }
        if (order.Status == OrderStatus.Pending) order.Status = OrderStatus.InProgress;
        await _db.SaveChangesAsync(ct);

        var saved = await LoadOrderAsync(order.Id, ct);
        return Ok(ToDto(saved!));
    }

    /// <summary>
    /// 反结账：撤销已完成订单的结账状态，回到 InProgress，本次冲销结账影响。
    /// </summary>
    [HttpPost("{id:long}/reopen")]
    public async Task<ActionResult<OrderDto>> Reopen(long id, [FromBody] ReopenOrderRequest req, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var order = await _db.Orders.Include(o => o.Items).Include(o => o.Member)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.Completed)
            return Conflict(new { code = "InvalidState", message = "仅已结账订单可反结账" });
        if (order.CompletedAt.HasValue && await IsDayClosedAsync(order.StoreId, order.CompletedAt.Value, ct))
            return Conflict(new { code = "DayClosed", message = "结账当日已日结，不能反结账" });

        if (order.PayMethod == PayMethod.MemberCard && order.Member is not null)
        {
            order.Member.Balance += order.PaidAmount;
            order.Member.TotalConsumed -= order.PaidAmount;
            if (order.Member.TotalConsumed < 0) order.Member.TotalConsumed = 0;
        }
        foreach (var it in order.Items) it.CommissionAmount = 0m;
        order.PaidAmount = 0m;
        order.DiscountAmount = 0m;
        order.PayMethod = PayMethod.Unpaid;
        order.Status = OrderStatus.InProgress;
        order.CompletedAt = null;
        order.ReopenedAt = DateTime.UtcNow;
        order.ReopenedByUserId = _tenantContext.UserId;
        order.ReopenReason = req.Reason;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogWarning("Order {OrderId} REOPENED by {UserId} reason={Reason}", id, _tenantContext.UserId, req.Reason);
        var saved = await LoadOrderAsync(id, ct);
        return Ok(ToDto(saved!));
    }

    /// <summary>
    /// 设置/调整订单小费（不计入营业额）。允许整单总额 + 按 item 平摊；
    /// 也允许结账后追加（小费不影响日结现金核对，只用于技师结算）。
    /// </summary>
    [HttpPost("{id:long}/tip")]
    public async Task<ActionResult<OrderDto>> SetTip(long id, [FromBody] SetTipRequest req, CancellationToken ct)
    {
        if (req.TipAmount < 0) return BadRequest(new { code = "InvalidTip", message = "小费不能为负" });
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return NotFound();
        if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Refunded)
            return Conflict(new { code = "InvalidState", message = "已取消/退款订单不可设小费" });

        order.TipAmount = req.TipAmount;
        var n = order.Items.Count;
        if (n > 0)
        {
            var share = Math.Round(req.TipAmount / n, 2);
            decimal allocated = 0m;
            for (var i = 0; i < n; i++)
            {
                var item = order.Items.ElementAt(i);
                item.TipAmount = (i == n - 1) ? Math.Round(req.TipAmount - allocated, 2) : share;
                allocated += item.TipAmount;
            }
        }
        await _db.SaveChangesAsync(ct);
        var saved = await LoadOrderAsync(id, ct);
        return Ok(ToDto(saved!));
    }

    private async Task<List<MemberPackage>> LoadActivePackagesAsync(long? memberId, CancellationToken ct)
    {
        if (!memberId.HasValue) return new();
        var now = DateTime.UtcNow;
        return await _db.MemberPackages
            .Where(p => p.MemberId == memberId.Value
                        && p.Status == MemberPackageStatus.Active
                        && (p.ExpiresAt == null || p.ExpiresAt > now)
                        && p.RemainCount > 0)
            .OrderBy(p => p.ExpiresAt ?? DateTime.MaxValue)
            .ToListAsync(ct);
    }

    /// <summary>尝试从可用套餐中消费 N 次；成功返回套餐并扣减次数，失败返回 null。</summary>
    private static MemberPackage? TryConsumePackage(List<MemberPackage> packages, long serviceId, int qty)
    {
        var hit = packages.FirstOrDefault(p =>
            (p.Kind == MemberPackageKind.Counter && p.ServiceId == serviceId && p.RemainCount >= qty)
            || (p.Kind == MemberPackageKind.Period && (p.ServiceId == null || p.ServiceId == serviceId) && p.RemainCount >= qty));
        if (hit is null) return null;
        hit.RemainCount -= qty;
        if (hit.RemainCount == 0) hit.Status = MemberPackageStatus.Used;
        return hit;
    }

    private async Task<TechnicianLevel> ResolveTechLevelAsync(long technicianId, CancellationToken ct) =>
        await _db.Users.AsNoTracking()
            .Where(u => u.Id == technicianId)
            .Select(u => (TechnicianLevel?)u.TechnicianLevel)
            .FirstOrDefaultAsync(ct) ?? TechnicianLevel.Senior;

    private static decimal ResolvePrice(ServiceItem svc, Member? member, TechnicianLevel level)
    {
        decimal basePrice = level switch
        {
            TechnicianLevel.Junior => svc.PriceJunior ?? svc.Price,
            TechnicianLevel.Master => svc.PriceMaster ?? svc.Price,
            _ => svc.Price
        };
        if (member is null) return basePrice;
        // 会员场景：取会员价（若有）与级别价中的低者
        var memberBase = svc.MemberPrice > 0m ? svc.MemberPrice : basePrice;
        return Math.Min(memberBase, basePrice);
    }

    [HttpPost("{id:long}/checkout")]
    public async Task<ActionResult<OrderDto>> Checkout(long id, [FromBody] CheckoutRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<PayMethod>(req.PayMethod, true, out var method) || method == PayMethod.Unpaid)
            return BadRequest(new { code = "InvalidPayMethod", message = "支付方式不合法" });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Member)
                .ThenInclude(m => m!.MemberType)
                    .ThenInclude(t => t!.ServiceItem)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return NotFound();
        if (await IsDayClosedAsync(order.StoreId, DateTime.UtcNow, ct))
            return Conflict(new { code = "DayClosed", message = "今日已日结，不能再结账" });
        if (order.Status == OrderStatus.Completed)
            return Conflict(new { code = "AlreadyCompleted", message = "订单已结账" });
        if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Refunded)
            return Conflict(new { code = "OrderInvalid", message = "订单已取消或已退款" });

        // 结算所有挂在该订单上的计时房费 session：按真实分钟数收尾，金额并入 Order.Total
        var linkedSessions = await _db.TimedRoomSessions
            .Where(s => s.OrderId == order.Id)
            .ToListAsync(ct);
        decimal roomTotal = 0m;
        var settlementTime = DateTime.UtcNow;
        foreach (var s in linkedSessions)
        {
            if (s.Status != TimedRoomSessionStatus.Open)
                return Conflict(new { code = "RoomSessionNotOpen", message = $"计时房 {s.RoomId} 已结算或作废，请重建订单" });
            var minutes = Math.Max(1, (int)Math.Ceiling((settlementTime - s.StartedAt).TotalMinutes));
            var amount = Math.Round(minutes / 60m * s.HourlyRateSnapshot, 2, MidpointRounding.AwayFromZero);
            s.EndedAt = settlementTime;
            s.BilledMinutes = minutes;
            s.Amount = amount;
            roomTotal += amount;
        }
        // 把房费汇总折回 Total（覆盖 Create 时的预估值，避免计时漂移）
        var itemTotal = order.Items.Sum(i => i.ItemTotal);
        order.Total = itemTotal + roomTotal;

        // 次卡-服务匹配校验：次卡(CountBased) 只能在订单含其绑定的服务项目时才允许参与结算，
        // 否则没有任何业务意义（次卡余额一般为 0，且无对应服务可扣次）。
        // 校验范围：主会员 + 合并结算的次要会员；命中即返回 400 拒绝。
        var settlementMemberIds = new List<long>();
        if (order.MemberId.HasValue) settlementMemberIds.Add(order.MemberId.Value);
        if (req.SecondaryMemberIds is { Count: > 0 })
            settlementMemberIds.AddRange(req.SecondaryMemberIds.Where(mid => mid != order.MemberId));
        if (settlementMemberIds.Count > 0)
        {
            var cartServiceIds = order.Items.Select(i => i.ServiceId).ToHashSet();
            var countBasedCards = await _db.Members.AsNoTracking()
                .Include(m => m.MemberType).ThenInclude(t => t!.ServiceItem)
                .Where(m => settlementMemberIds.Contains(m.Id)
                            && m.MemberType != null
                            && m.MemberType.Kind == MemberTypeKind.CountBased)
                .ToListAsync(ct);
            foreach (var card in countBasedCards)
            {
                var boundSvcId = card.MemberType!.ServiceItemId;
                if (boundSvcId is null || !cartServiceIds.Contains(boundSvcId.Value))
                {
                    var svcName = card.MemberType.ServiceItem?.Name ?? "（未绑定服务）";
                    return BadRequest(new
                    {
                        code = "PunchCardMismatch",
                        message = $"次卡 {card.CardNo} 绑定的服务「{svcName}」不在本单内，不能参与结算"
                    });
                }
            }
        }

        var discount = req.DiscountAmount < 0 ? 0 : req.DiscountAmount;
        // 注：会员折扣已经在 开卡/充值 时通过现金价（充值金额 × 折扣）兑现，
        // 结账时不再按 Member.Discount 重复打折；订单线的单价已使用 MemberPrice。
        // 券折扣：取面值 or 折扣百分比中的有效值
        if (order.VoucherId.HasValue)
        {
            var v = await _db.Vouchers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.VoucherId.Value, ct);
            if (v is not null)
            {
                var voucherDiscount = v.DiscountPercent.HasValue
                    ? Math.Round(order.Total * (1m - v.DiscountPercent.Value), 2)
                    : v.FaceValue;
                discount = Math.Max(discount, voucherDiscount);
            }
        }
        if (discount > order.Total)
            return BadRequest(new { code = "InvalidDiscount", message = "优惠不能超过订单总额" });

        var paid = order.Total - discount;
        if (req.PaidAmount.HasValue && req.PaidAmount.Value < paid)
            return BadRequest(new { code = "InsufficientAmount", message = "实收金额不足" });

        if (method == PayMethod.MemberCard)
        {
            if (order.Member is null)
                return BadRequest(new { code = "NoMember", message = "未关联会员，无法用会员卡结账" });

            // 合并结算：把主卡 + 次要卡按顺序扣余额（次要卡必须同手机号、未关闭）
            var cards = new List<Member> { order.Member };
            if (req.SecondaryMemberIds is { Count: > 0 })
            {
                var ids = req.SecondaryMemberIds.Distinct().Where(id => id != order.Member.Id).ToList();
                if (ids.Count > 0)
                {
                    var extras = await _db.Members
                        .Where(m => ids.Contains(m.Id) && m.IsActive && m.Phone == order.Member.Phone)
                        .ToListAsync(ct);
                    cards.AddRange(extras);
                }
            }

            var totalAvail = cards.Sum(c => c.Balance);
            if (totalAvail < paid)
                return BadRequest(new { code = "InsufficientBalance", message = $"合并余额不足，可用 ¥{totalAvail:F2}，应付 ¥{paid:F2}" });

            var remaining = paid;
            foreach (var c in cards)
            {
                if (remaining <= 0) break;
                var take = Math.Min(c.Balance, remaining);
                if (take <= 0) continue;
                c.Balance -= take;
                c.TotalConsumed += take;
                remaining -= take;
            }
        }
        else if (order.Member is not null)
        {
            order.Member.TotalConsumed += paid;
        }

        var rules = await _db.CommissionRules.AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync(ct);

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var techCounts = new Dictionary<long, int>();
        foreach (var item in order.Items)
        {
            if (!techCounts.ContainsKey(item.TechnicianId))
            {
                techCounts[item.TechnicianId] = await _db.OrderItems.AsNoTracking()
                    .Where(oi => oi.TechnicianId == item.TechnicianId
                                 && oi.Order.Status == OrderStatus.Completed
                                 && oi.Order.CompletedAt >= monthStart)
                    .SumAsync(oi => (int?)oi.Quantity, ct) ?? 0;
            }
            techCounts[item.TechnicianId] += item.Quantity;

            item.CommissionAmount = CommissionCalculator.Compute(
                rules, item.TechnicianId, item.ServiceId,
                item.ItemTotal, item.DurationMinutes * item.Quantity,
                techCounts[item.TechnicianId],
                item.AssignmentSource);
        }

        order.PayMethod = method;
        order.DiscountAmount = discount;
        order.PaidAmount = paid;
        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTime.UtcNow;

        // 计时房 session 跟随订单收尾：支付方式与订单一致，状态推到已结算
        foreach (var s in linkedSessions)
        {
            s.PayMethod = method;
            s.Status = TimedRoomSessionStatus.Settled;
        }
        if (!string.IsNullOrWhiteSpace(req.Remark))
            order.Remark = (order.Remark ?? string.Empty) + " | 结账备注: " + req.Remark;

        var queueRows = await _db.TechnicianQueues
            .Where(q => q.StoreId == order.StoreId && order.Items.Select(i => i.TechnicianId).Distinct().Contains(q.TechnicianId))
            .ToListAsync(ct);
        foreach (var row in queueRows)
        {
            row.TodayRoundCount += 1;
            row.LastCalledAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation(
            "Order {OrderId} checked out: total={Total} discount={Discount} paid={Paid} method={Method}",
            order.Id, order.Total, discount, paid, method);

        var saved = await LoadOrderAsync(order.Id, ct);
        return Ok(ToDto(saved!));
    }

    [HttpPost("{id:long}/refund")]
    public async Task<ActionResult<OrderDto>> Refund(long id, [FromBody] RefundRequest req, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Member)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.Completed)
            return BadRequest(new { code = "NotCompleted", message = "只有已完成的订单可退款" });
        if (order.CompletedAt.HasValue && await IsDayClosedAsync(order.StoreId, order.CompletedAt.Value, ct))
            return Conflict(new { code = "DayClosed", message = "结账当日已日结，不能退款" });

        if (order.PayMethod == PayMethod.MemberCard && order.Member is not null)
        {
            order.Member.Balance += order.PaidAmount;
            order.Member.TotalConsumed -= order.PaidAmount;
            if (order.Member.TotalConsumed < 0) order.Member.TotalConsumed = 0;
        }

        foreach (var it in order.Items) it.CommissionAmount = 0m;

        order.Status = OrderStatus.Refunded;
        order.Remark = (order.Remark ?? string.Empty) + " | 退款原因: " + (req.Reason ?? "无");

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var saved = await LoadOrderAsync(order.Id, ct);
        return Ok(ToDto(saved!));
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.InProgress)
            return BadRequest(new { code = "InvalidState", message = "仅未结账订单可取消" });
        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// 转钟：把某个 OrderItem 的技师换成新技师。仅 Pending/InProgress 订单可转。
    /// </summary>
    [HttpPatch("{orderId:long}/items/{itemId:long}/transfer")]
    public async Task<ActionResult<OrderDto>> TransferTechnician(
        long orderId, long itemId,
        [FromBody] TransferTechnicianRequest req,
        CancellationToken ct)
    {
        var order = await _db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.InProgress)
            return Conflict(new { code = "InvalidState", message = "仅未结账订单可转钟" });

        var item = order.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return NotFound(new { code = "ItemNotFound", message = "订单项不存在" });
        if (item.TechnicianId == req.NewTechnicianId)
            return BadRequest(new { code = "SameTechnician", message = "新技师与原技师相同" });

        var newTech = await _db.Users.FirstOrDefaultAsync(u =>
            u.Id == req.NewTechnicianId && u.Role == UserRole.Technician && u.IsActive, ct);
        if (newTech is null) return BadRequest(new { code = "TechnicianNotFound", message = "新技师不存在或已停用" });

        // AssignmentSource 在转钟时保持不变：来源标记的是"客人是怎么找到第一位技师的"，
        // 中途换人不改变这个来源。新技师的"工作量计入轮/点"通过 OrderItem 的 source 字段直接生效。
        item.PreviousTechnicianId = item.TechnicianId;
        item.TechnicianId = req.NewTechnicianId;
        item.TransferredAt = DateTime.UtcNow;
        item.TransferReason = req.Reason;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Order {OrderId} item {ItemId} transferred from tech {From} to tech {To} (reason: {Reason})",
            orderId, itemId, item.PreviousTechnicianId, item.TechnicianId, req.Reason);

        var saved = await LoadOrderAsync(orderId, ct);
        return Ok(ToDto(saved!));
    }

    /// <summary>
    /// 并钟：把多个未结账订单项标记为"同一技师同时服务多位客人"。
    /// 要求 ≥ 2 项、同一技师、订单均未结账。提成不变，仅做分组标记。
    /// </summary>
    [HttpPost("items/merge")]
    public async Task<ActionResult<IReadOnlyList<long>>> MergeItems(
        [FromBody] MergeOrderItemsRequest req, CancellationToken ct)
    {
        var ids = req.OrderItemIds?.Distinct().ToList() ?? new List<long>();
        if (ids.Count < 2)
            return BadRequest(new { code = "TooFew", message = "并钟至少需要 2 个订单项" });

        var items = await _db.OrderItems.Include(i => i.Order)
            .Where(i => ids.Contains(i.Id)).ToListAsync(ct);
        if (items.Count != ids.Count)
            return NotFound(new { code = "ItemNotFound", message = "部分订单项不存在" });

        if (items.Select(i => i.TechnicianId).Distinct().Count() != 1)
            return BadRequest(new { code = "DifferentTechnician", message = "并钟的订单项必须是同一技师" });
        if (items.Any(i => i.Order.Status != OrderStatus.Pending && i.Order.Status != OrderStatus.InProgress))
            return Conflict(new { code = "InvalidState", message = "仅未结账订单可并钟" });
        if (items.Any(i => i.MergedGroupKey != null))
            return Conflict(new { code = "AlreadyMerged", message = "存在已并钟的订单项，请先取消并钟" });

        var groupKey = Guid.NewGuid().ToString();
        foreach (var i in items) i.MergedGroupKey = groupKey;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Order items merged group={Group} items={Items} tech={Tech}",
            groupKey, string.Join(",", ids), items[0].TechnicianId);
        return Ok(ids);
    }

    /// <summary>取消并钟：清除某订单项的并钟标记。若该组只剩 1 项，连带清除以免孤立。</summary>
    [HttpPost("items/{itemId:long}/unmerge")]
    public async Task<IActionResult> UnmergeItem(long itemId, CancellationToken ct)
    {
        var item = await _db.OrderItems.FirstOrDefaultAsync(i => i.Id == itemId, ct);
        if (item is null) return NotFound();
        if (item.MergedGroupKey is null)
            return BadRequest(new { code = "NotMerged", message = "该订单项未并钟" });

        var groupKey = item.MergedGroupKey;
        item.MergedGroupKey = null;

        var remaining = await _db.OrderItems
            .Where(i => i.MergedGroupKey == groupKey && i.Id != itemId).ToListAsync(ct);
        if (remaining.Count == 1) remaining[0].MergedGroupKey = null;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// 判断目标时刻所属的业务日是否已经做日结：已结过则不允许新增/修改/退款。
    /// 业务日按门店配置的切日分钟数（DayCloseCutoffMinutes）划分。
    /// </summary>
    private async Task<bool> IsDayClosedAsync(long storeId, DateTime when, CancellationToken ct)
    {
        var cutoff = await _db.Stores.AsNoTracking()
            .Where(s => s.Id == storeId)
            .Select(s => (int?)s.DayCloseCutoffMinutes)
            .FirstOrDefaultAsync(ct) ?? 0;
        var businessDate = MassageSaas.Domain.Common.BusinessDayCalculator.BusinessDateOf(when, cutoff);
        return await _db.DayCloses.AnyAsync(d => d.StoreId == storeId && d.BusinessDate == businessDate, ct);
    }

    private async Task<Order?> LoadOrderAsync(long id, CancellationToken ct) =>
        await _db.Orders.AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Technician)
            .Include(o => o.Member)
                .ThenInclude(m => m!.MemberType)
            .Include(o => o.CashierUser)
            .Include(o => o.RoomSessions).ThenInclude(s => s.Room)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    private static OrderDto ToDto(Order o)
    {
        var items = o.Items.Select(i =>
        {
            var listAmount = Math.Round(i.ListUnitPrice * i.Quantity, 2);
            return new OrderItemDto(
                i.Id, i.ServiceId, i.ServiceName, i.TechnicianId,
                i.Technician != null ? (i.Technician.RealName ?? i.Technician.Username) : null,
                i.Quantity, i.DurationMinutes, i.UnitPrice, i.ItemTotal,
                i.CommissionAmount, i.RoomId, i.RoomNoSnapshot,
                i.PreviousTechnicianId, i.TransferredAt,
                i.TipAmount, i.MemberPackageId, i.IsAddOn, i.MergedGroupKey,
                i.ListUnitPrice, listAmount,
                i.AssignmentSource.ToString());
        }).ToList();
        var roomCharges = o.RoomSessions
            .OrderBy(s => s.StartedAt)
            .Select(s => new OrderRoomChargeDto(
                s.Id, s.RoomId, s.Room?.RoomNo ?? string.Empty,
                // 未结算时显示当前预估分钟，已结算用实际记账分钟
                s.BilledMinutes > 0 ? s.BilledMinutes
                    : Math.Max(1, (int)Math.Ceiling(((s.EndedAt ?? DateTime.UtcNow) - s.StartedAt).TotalMinutes)),
                s.HourlyRateSnapshot, s.Amount,
                s.PayMethod.ToString(), s.Status.ToString()))
            .ToList();
        var listTotal = items.Sum(x => x.ListAmount) + roomCharges.Sum(x => x.Amount);
        var punchCount = o.Items.Where(i => i.MemberPackageId.HasValue).Sum(i => i.Quantity);
        return new OrderDto(
            o.Id, o.OrderNo, o.StoreId, o.MemberId,
            o.Member?.CardNo,
            o.Total, o.DiscountAmount, o.PaidAmount, o.TipAmount,
            o.PayMethod.ToString(), o.Status.ToString(),
            o.CreatedAt, o.StartedAt, o.CompletedAt,
            o.CashierUserId,
            o.CashierUser != null ? (o.CashierUser.RealName ?? o.CashierUser.Username) : null,
            o.Remark, o.VoucherId, o.ReopenedAt, o.ReopenReason,
            items, listTotal, punchCount,
            MemberPhone: o.Member?.Phone,
            MemberName: o.Member?.Name,
            MemberTypeName: o.Member?.MemberType?.Name,
            MemberTypeKind: o.Member?.MemberType?.Kind.ToString(),
            RoomCharges: roomCharges);
    }

    private static string GenerateOrderNo() =>
        $"O{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
}
