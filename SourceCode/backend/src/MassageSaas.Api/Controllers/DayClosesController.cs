using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.DayCloses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/day-closes")]
[Authorize(Policy = "ShopStaff")]
public class DayClosesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DayClosesController> _logger;

    public DayClosesController(ApplicationDbContext db, ITenantContext tenantContext, ILogger<DayClosesController> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// 日结预览：计算指定日期的应收/各支付方式分布。
    /// </summary>
    [HttpGet("preview")]
    public async Task<ActionResult<DayClosePreviewDto>> Preview(
        [FromQuery] long storeId,
        [FromQuery] DateTime? date = null,
        CancellationToken ct = default)
    {
        var d = (date ?? DateTime.UtcNow).Date;
        var dateOnly = DateOnly.FromDateTime(d);
        var start = DateTime.SpecifyKind(d, DateTimeKind.Utc);
        var end = start.AddDays(1);

        var alreadyClosed = await _db.DayCloses.AnyAsync(x => x.StoreId == storeId && x.BusinessDate == dateOnly, ct);

        var orders = _db.Orders.AsNoTracking()
            .Where(o => o.StoreId == storeId
                        && o.Status == OrderStatus.Completed
                        && o.CompletedAt >= start && o.CompletedAt < end);

        var orderCount = await orders.CountAsync(ct);
        var revenue = await orders.SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;
        var cash = await orders.Where(o => o.PayMethod == PayMethod.Cash).SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;
        var card = await orders.Where(o => o.PayMethod == PayMethod.MemberCard).SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;
        var wechat = await orders.Where(o => o.PayMethod == PayMethod.Wechat).SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;
        var alipay = await orders.Where(o => o.PayMethod == PayMethod.Alipay).SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;
        var bank = await orders.Where(o => o.PayMethod == PayMethod.BankCard).SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;

        // 计时房已结算收入：按结算时间（EndedAt）归当日，并入各支付方式与营业额。
        // 已挂到订单（OrderId != null）的 session 不再重复计入：其金额已包含在 Orders.PaidAmount 中。
        var timed = _db.TimedRoomSessions.AsNoTracking()
            .Where(s => s.StoreId == storeId
                        && s.Status == TimedRoomSessionStatus.Settled
                        && s.OrderId == null
                        && s.EndedAt != null && s.EndedAt >= start && s.EndedAt < end);
        revenue += await timed.SumAsync(s => (decimal?)s.Amount, ct) ?? 0m;
        cash += await timed.Where(s => s.PayMethod == PayMethod.Cash).SumAsync(s => (decimal?)s.Amount, ct) ?? 0m;
        card += await timed.Where(s => s.PayMethod == PayMethod.MemberCard).SumAsync(s => (decimal?)s.Amount, ct) ?? 0m;
        wechat += await timed.Where(s => s.PayMethod == PayMethod.Wechat).SumAsync(s => (decimal?)s.Amount, ct) ?? 0m;
        alipay += await timed.Where(s => s.PayMethod == PayMethod.Alipay).SumAsync(s => (decimal?)s.Amount, ct) ?? 0m;
        bank += await timed.Where(s => s.PayMethod == PayMethod.BankCard).SumAsync(s => (decimal?)s.Amount, ct) ?? 0m;

        var rechargeCash = await _db.MemberRechargeRecords.AsNoTracking()
            .Where(r => r.StoreId == storeId && r.CreatedAt >= start && r.CreatedAt < end
                        && r.PayMethod == PayMethod.Cash)
            .SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;
        var rechargeAll = await _db.MemberRechargeRecords.AsNoTracking()
            .Where(r => r.StoreId == storeId && r.CreatedAt >= start && r.CreatedAt < end)
            .SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;

        return Ok(new DayClosePreviewDto(
            d, storeId, orderCount, revenue,
            ExpectedCash: cash + rechargeCash,
            cash, card, wechat, alipay, bank, rechargeAll, alreadyClosed));
    }

    [HttpPost]
    public async Task<ActionResult<DayCloseDto>> Submit([FromBody] SubmitDayCloseRequest req, CancellationToken ct)
    {
        var date = DateOnly.FromDateTime(req.BusinessDate);
        if (await _db.DayCloses.AnyAsync(x => x.StoreId == req.StoreId && x.BusinessDate == date, ct))
            return Conflict(new { code = "AlreadyClosed", message = "该日已日结，不能重复提交" });

        var preview = await Preview(req.StoreId, req.BusinessDate, ct);
        if (preview.Result is not OkObjectResult ok || ok.Value is not DayClosePreviewDto p)
            return BadRequest();

        var entity = new DayClose
        {
            StoreId = req.StoreId,
            BusinessDate = date,
            ExpectedCash = p.ExpectedCash,
            ActualCash = req.ActualCash,
            Variance = req.ActualCash - p.ExpectedCash,
            RevenueTotal = p.RevenueTotal,
            CashAmount = p.CashAmount,
            MemberCardAmount = p.MemberCardAmount,
            WechatAmount = p.WechatAmount,
            AlipayAmount = p.AlipayAmount,
            BankCardAmount = p.BankCardAmount,
            RechargeAmount = p.RechargeAmount,
            OrderCount = p.OrderCount,
            OperatorUserId = _tenantContext.UserId,
            Remark = req.Remark
        };
        _db.DayCloses.Add(entity);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "DayClose submitted store={StoreId} date={Date:yyyy-MM-dd} expected={Expected} actual={Actual} variance={Variance}",
            req.StoreId, date, entity.ExpectedCash, entity.ActualCash, entity.Variance);

        return Ok(await ToDtoAsync(entity, ct));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DayCloseDto>>> History(
        [FromQuery] long storeId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var q = _db.DayCloses.AsNoTracking()
            .Include(x => x.OperatorUser)
            .Where(x => x.StoreId == storeId);
        if (from.HasValue) q = q.Where(x => x.BusinessDate >= DateOnly.FromDateTime(from.Value));
        if (to.HasValue) q = q.Where(x => x.BusinessDate <= DateOnly.FromDateTime(to.Value));

        var rows = await q.OrderByDescending(x => x.BusinessDate).Take(60).ToListAsync(ct);
        return Ok(rows.Select(MapDto).ToList());
    }

    private async Task<DayCloseDto> ToDtoAsync(DayClose e, CancellationToken ct)
    {
        if (e.OperatorUser is null && e.OperatorUserId.HasValue)
        {
            e.OperatorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == e.OperatorUserId, ct);
        }
        return MapDto(e);
    }

    private static DayCloseDto MapDto(DayClose e) => new(
        e.Id, e.StoreId,
        e.BusinessDate.ToDateTime(TimeOnly.MinValue),
        e.OrderCount, e.RevenueTotal,
        e.ExpectedCash, e.ActualCash, e.Variance,
        e.CashAmount, e.MemberCardAmount, e.WechatAmount, e.AlipayAmount, e.BankCardAmount, e.RechargeAmount,
        e.OperatorUserId,
        e.OperatorUser != null ? (e.OperatorUser.RealName ?? e.OperatorUser.Username) : null,
        e.Remark, e.CreatedAt);
}
