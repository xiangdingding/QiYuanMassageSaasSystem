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

        decimal total = 0m;
        foreach (var input in req.Items)
        {
            var svc = services[input.ServiceId];
            var qty = input.Quantity < 1 ? 1 : input.Quantity;
            var unit = member is not null ? svc.MemberPrice : svc.Price;
            var lineTotal = Math.Round(unit * qty, 2);
            total += lineTotal;

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
                RoomNo = input.RoomNo
            });
        }
        order.Total = total;
        order.PaidAmount = 0m;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        var saved = await LoadOrderAsync(order.Id, ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, ToDto(saved!));
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
        if (order.Status == OrderStatus.Completed)
            return Conflict(new { code = "AlreadyCompleted", message = "订单已结账" });
        if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Refunded)
            return Conflict(new { code = "OrderInvalid", message = "订单已取消或已退款" });

        var discount = req.DiscountAmount < 0 ? 0 : req.DiscountAmount;
        if (order.Member is not null)
            discount = Math.Max(discount, Math.Round(order.Total * (1 - order.Member.Discount), 2));
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

    private async Task<Order?> LoadOrderAsync(long id, CancellationToken ct) =>
        await _db.Orders.AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Technician)
            .Include(o => o.Member)
            .Include(o => o.CashierUser)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    private static OrderDto ToDto(Order o) => new(
        o.Id, o.OrderNo, o.StoreId, o.MemberId,
        o.Member?.CardNo,
        o.Total, o.DiscountAmount, o.PaidAmount,
        o.PayMethod.ToString(), o.Status.ToString(),
        o.CreatedAt, o.StartedAt, o.CompletedAt,
        o.CashierUserId,
        o.CashierUser != null ? (o.CashierUser.RealName ?? o.CashierUser.Username) : null,
        o.Remark,
        o.Items.Select(i => new OrderItemDto(
            i.Id, i.ServiceId, i.ServiceName, i.TechnicianId,
            i.Technician != null ? (i.Technician.RealName ?? i.Technician.Username) : null,
            i.Quantity, i.DurationMinutes, i.UnitPrice, i.ItemTotal,
            i.CommissionAmount, i.RoomNo)).ToList());

    private static string GenerateOrderNo() =>
        $"O{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
}
