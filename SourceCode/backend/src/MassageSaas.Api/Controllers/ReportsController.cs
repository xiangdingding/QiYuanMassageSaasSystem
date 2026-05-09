using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ReportsController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet("daily")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<DailyReportDto>> Daily(
        [FromQuery] long storeId,
        [FromQuery] DateTime? date = null,
        CancellationToken ct = default)
    {
        var d = (date ?? DateTime.UtcNow).Date;
        var start = DateTime.SpecifyKind(d, DateTimeKind.Utc);
        var end = start.AddDays(1);

        var orders = _db.Orders.AsNoTracking()
            .Where(o => o.StoreId == storeId && o.CreatedAt >= start && o.CreatedAt < end);

        var completed = orders.Where(o => o.Status == OrderStatus.Completed);
        var refunded = orders.Where(o => o.Status == OrderStatus.Refunded);

        var orderCount = await completed.CountAsync(ct);
        var revenue = await completed.SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;
        var cash = await completed.Where(o => o.PayMethod == PayMethod.Cash).SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;
        var card = await completed.Where(o => o.PayMethod == PayMethod.MemberCard).SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;
        var wechat = await completed.Where(o => o.PayMethod == PayMethod.Wechat).SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;
        var alipay = await completed.Where(o => o.PayMethod == PayMethod.Alipay).SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;
        var bank = await completed.Where(o => o.PayMethod == PayMethod.BankCard).SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;

        var refundCount = await refunded.CountAsync(ct);
        var refundAmount = await refunded.SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;

        var recharges = _db.MemberRechargeRecords.AsNoTracking()
            .Where(r => r.StoreId == storeId && r.CreatedAt >= start && r.CreatedAt < end);
        var rechargeCount = await recharges.CountAsync(ct);
        var rechargeAmount = await recharges.SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;

        return Ok(new DailyReportDto(
            d, storeId, orderCount, revenue,
            cash, card, wechat, alipay, bank,
            refundCount, refundAmount,
            rechargeCount, rechargeAmount));
    }

    [HttpGet("technician-performance")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<IReadOnlyList<TechnicianPerformanceDto>>> TechnicianPerformance(
        [FromQuery] long storeId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct = default)
    {
        if (to <= from) return BadRequest(new { code = "InvalidRange", message = "结束时间必须大于开始时间" });

        var data = await _db.OrderItems.AsNoTracking()
            .Where(oi =>
                oi.Order.StoreId == storeId
                && oi.Order.Status == OrderStatus.Completed
                && oi.Order.CompletedAt >= from
                && oi.Order.CompletedAt < to)
            .GroupBy(oi => new { oi.TechnicianId, oi.Technician.RealName, oi.Technician.Username, oi.Technician.EmployeeNo })
            .Select(g => new TechnicianPerformanceDto(
                g.Key.TechnicianId,
                g.Key.RealName ?? g.Key.Username,
                g.Key.EmployeeNo,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.ItemTotal),
                g.Sum(x => x.CommissionAmount),
                g.Sum(x => x.DurationMinutes * x.Quantity)))
            .OrderByDescending(d => d.TotalCommission)
            .ToListAsync(ct);

        return Ok(data);
    }

    /// <summary>
    /// 当前登录技师查看自己的业绩。给小程序"我的业绩"页用。
    /// </summary>
    [HttpGet("me/performance")]
    public async Task<ActionResult<MyPerformanceDto>> MyPerformance(CancellationToken ct)
    {
        if (_tenantContext.UserId is not long uid)
            return Unauthorized();

        var now = DateTime.UtcNow;
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var baseQ = _db.OrderItems.AsNoTracking()
            .Where(oi => oi.TechnicianId == uid
                         && oi.Order.Status == OrderStatus.Completed
                         && oi.Order.CompletedAt != null);

        var todayQ = baseQ.Where(oi => oi.Order.CompletedAt >= todayStart);
        var monthQ = baseQ.Where(oi => oi.Order.CompletedAt >= monthStart);

        var todayAmount = await todayQ.SumAsync(x => (decimal?)x.ItemTotal, ct) ?? 0m;
        var todayCommission = await todayQ.SumAsync(x => (decimal?)x.CommissionAmount, ct) ?? 0m;
        var todayRoundCount = await todayQ.SumAsync(x => (int?)x.Quantity, ct) ?? 0;
        var monthAmount = await monthQ.SumAsync(x => (decimal?)x.ItemTotal, ct) ?? 0m;
        var monthCommission = await monthQ.SumAsync(x => (decimal?)x.CommissionAmount, ct) ?? 0m;
        var monthRoundCount = await monthQ.SumAsync(x => (int?)x.Quantity, ct) ?? 0;

        return Ok(new MyPerformanceDto(
            todayAmount, todayCommission,
            monthAmount, monthCommission,
            todayRoundCount, monthRoundCount));
    }
}
