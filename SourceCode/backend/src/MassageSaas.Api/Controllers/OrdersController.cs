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
                o.Items.Count, o.CreatedAt, o.CompletedAt,
                o.Member != null ? o.Member.CardNo : null))
            .ToListAsync(ct);

        return Ok(new PagedResult<OrderListItemDto>(items, total, pq.SafePage, pq.SafePageSize));
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
        if (req.Items is null || req.Items.Count == 0)
            return BadRequest(new { code = "InvalidInput", message = "至少需要一个服务项" });

        var serviceIds = req.Items.Select(i => i.ServiceId).Distinct().ToList();
        var techIds = req.Items.Select(i => i.TechnicianId).Distinct().ToList();

        var services = await _db.ServiceItems.AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id) && s.IsActive)
            .ToDictionaryAsync(s => s.Id, ct);
        if (services.Count != serviceIds.Count)
            return BadRequest(new { code = "ServiceNotFound", message = "存在未启用或不存在的服务项" });

        var techCount = await _db.Users.CountAsync(u =>
            techIds.Contains(u.Id) && u.Role == UserRole.Technician && u.IsActive, ct);
        if (techCount != techIds.Count)
            return BadRequest(new { code = "TechnicianNotFound", message = "存在不存在或已停用的技师" });

        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == req.StoreId && s.IsActive, ct);
        if (store is null) return BadRequest(new { code = "StoreNotFound", message = "门店不存在或已停用" });

        // 房间校验：必须属于该门店、启用、且未被进行中订单占用
        var roomIds = req.Items.Where(i => i.RoomId.HasValue).Select(i => i.RoomId!.Value).Distinct().ToList();
        Dictionary<long, Room> rooms = new();
        if (roomIds.Count > 0)
        {
            var rs = await _db.Rooms.AsNoTracking()
                .Where(r => roomIds.Contains(r.Id) && r.StoreId == req.StoreId && r.IsActive)
                .ToListAsync(ct);
            if (rs.Count != roomIds.Count)
                return BadRequest(new { code = "RoomInvalid", message = "存在不属于该门店或已停用的房间" });
            rooms = rs.ToDictionary(r => r.Id);

            var occupiedNow = await _db.OrderItems.AsNoTracking()
                .Where(oi => oi.RoomId != null && roomIds.Contains(oi.RoomId.Value)
                             && (oi.Order.Status == OrderStatus.Pending || oi.Order.Status == OrderStatus.InProgress))
                .Select(oi => oi.RoomId!.Value)
                .ToListAsync(ct);
            if (occupiedNow.Count > 0)
                return Conflict(new { code = "RoomOccupied", message = "选中的房间正被其他订单占用" });
        }

        Member? member = null;
        if (req.MemberId.HasValue)
        {
            member = await _db.Members.FirstOrDefaultAsync(x => x.Id == req.MemberId.Value, ct);
            if (member is null) return BadRequest(new { code = "MemberNotFound", message = "会员不存在" });
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
        foreach (var input in req.Items)
        {
            var svc = services[input.ServiceId];
            var qty = input.Quantity < 1 ? 1 : input.Quantity;

            var techLevel = await ResolveTechLevelAsync(input.TechnicianId, ct);
            var unit = ResolvePrice(svc, member, techLevel);
            decimal lineTotal = Math.Round(unit * qty, 2);

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
                CommissionAmount = 0m,
                RoomId = room?.Id,
                RoomNoSnapshot = room?.RoomNo,
                MemberPackageId = pkg?.Id,
                IsAddOn = false
            });
        }
        order.Total = total;
        order.PaidAmount = 0m;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        var saved = await LoadOrderAsync(order.Id, ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, ToDto(saved!));
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
                RoomId = input.RoomId,
                MemberPackageId = pkg?.Id,
                IsAddOn = true
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
            .FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return NotFound();
        if (await IsDayClosedAsync(order.StoreId, DateTime.UtcNow, ct))
            return Conflict(new { code = "DayClosed", message = "今日已日结，不能再结账" });
        if (order.Status == OrderStatus.Completed)
            return Conflict(new { code = "AlreadyCompleted", message = "订单已结账" });
        if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Refunded)
            return Conflict(new { code = "OrderInvalid", message = "订单已取消或已退款" });

        var discount = req.DiscountAmount < 0 ? 0 : req.DiscountAmount;
        if (order.Member is not null)
            discount = Math.Max(discount, Math.Round(order.Total * (1 - order.Member.Discount), 2));
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
            if (order.Member.Balance < paid)
                return BadRequest(new { code = "InsufficientBalance", message = "会员余额不足" });
            order.Member.Balance -= paid;
            order.Member.TotalConsumed += paid;
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
                techCounts[item.TechnicianId]);
        }

        order.PayMethod = method;
        order.DiscountAmount = discount;
        order.PaidAmount = paid;
        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTime.UtcNow;
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
    /// 判断目标日期是否已经做日结：已结过则不允许新增/修改/退款。
    /// </summary>
    private async Task<bool> IsDayClosedAsync(long storeId, DateTime when, CancellationToken ct)
    {
        var date = DateOnly.FromDateTime(when);
        return await _db.DayCloses.AnyAsync(d => d.StoreId == storeId && d.BusinessDate == date, ct);
    }

    private async Task<Order?> LoadOrderAsync(long id, CancellationToken ct) =>
        await _db.Orders.AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Technician)
            .Include(o => o.Member)
            .Include(o => o.CashierUser)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    private static OrderDto ToDto(Order o) => new(
        o.Id, o.OrderNo, o.StoreId, o.MemberId,
        o.Member?.CardNo,
        o.Total, o.DiscountAmount, o.PaidAmount, o.TipAmount,
        o.PayMethod.ToString(), o.Status.ToString(),
        o.CreatedAt, o.StartedAt, o.CompletedAt,
        o.CashierUserId,
        o.CashierUser != null ? (o.CashierUser.RealName ?? o.CashierUser.Username) : null,
        o.Remark, o.VoucherId, o.ReopenedAt, o.ReopenReason,
        o.Items.Select(i => new OrderItemDto(
            i.Id, i.ServiceId, i.ServiceName, i.TechnicianId,
            i.Technician != null ? (i.Technician.RealName ?? i.Technician.Username) : null,
            i.Quantity, i.DurationMinutes, i.UnitPrice, i.ItemTotal,
            i.CommissionAmount, i.RoomId, i.RoomNoSnapshot,
            i.PreviousTechnicianId, i.TransferredAt,
            i.TipAmount, i.MemberPackageId, i.IsAddOn)).ToList());

    private static string GenerateOrderNo() =>
        $"O{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
}
