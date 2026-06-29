using MassageSaas.Api.Reports;
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

    private async Task<int> GetCutoffAsync(long storeId, CancellationToken ct) =>
        await _db.Stores.AsNoTracking()
            .Where(s => s.Id == storeId)
            .Select(s => (int?)s.DayCloseCutoffMinutes)
            .FirstOrDefaultAsync(ct) ?? 0;

    [HttpGet("daily")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<DailyReportDto>> Daily(
        [FromQuery] long storeId,
        [FromQuery] DateTime? date = null,
        CancellationToken ct = default)
    {
        var cutoff = await GetCutoffAsync(storeId, ct);
        var businessDate = date.HasValue
            ? DateOnly.FromDateTime(date.Value)
            : BusinessDayCalculator.TodayBusinessDate(cutoff);
        var (start, end) = BusinessDayCalculator.RangeOf(businessDate, cutoff);
        var d = businessDate.ToDateTime(TimeOnly.MinValue);

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

        // 计时房已结算收入并入营业额与各支付方式（已挂订单的 session 跳过，避免与 Orders.PaidAmount 重算）
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

        // EF Core 8 + Pomelo 不能翻译"GroupBy + 多聚合 + Where(嵌套)"模式，
        // 故先把命中行投影成扁平结构拉到内存，再在 C# 端分组。
        var rows = await _db.OrderItems.AsNoTracking()
            .Where(oi =>
                oi.Order.StoreId == storeId
                && oi.Order.Status == OrderStatus.Completed
                && oi.Order.CompletedAt >= from
                && oi.Order.CompletedAt < to)
            .Select(oi => new
            {
                oi.TechnicianId,
                oi.Technician.RealName,
                oi.Technician.Username,
                oi.Technician.EmployeeNo,
                oi.Quantity,
                oi.ItemTotal,
                oi.CommissionAmount,
                oi.DurationMinutes,
                oi.AssignmentSource
            })
            .ToListAsync(ct);

        var data = rows
            .GroupBy(r => new { r.TechnicianId, r.RealName, r.Username, r.EmployeeNo })
            .Select(g =>
            {
                var designation = g.Where(x => x.AssignmentSource == AssignmentSource.Designation).Sum(x => x.Quantity);
                var rotation = g.Where(x => x.AssignmentSource == AssignmentSource.Rotation).Sum(x => x.Quantity);
                var denom = designation + rotation;
                decimal? rate = denom == 0
                    ? null
                    : Math.Round((decimal)designation / denom, 4);
                return new TechnicianPerformanceDto(
                    g.Key.TechnicianId,
                    g.Key.RealName ?? g.Key.Username,
                    g.Key.EmployeeNo,
                    g.Sum(x => x.Quantity),
                    g.Sum(x => x.ItemTotal),
                    g.Sum(x => x.CommissionAmount),
                    g.Sum(x => x.DurationMinutes * x.Quantity),
                    designation, rotation, rate);
            })
            .OrderByDescending(d => d.TotalCommission)
            .ToList();

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

        // 技师的"今日"/"本月"按其所在门店的切日时间口径
        var storeId = await _db.Users.AsNoTracking()
            .Where(u => u.Id == uid).Select(u => u.StoreId).FirstOrDefaultAsync(ct);
        var cutoff = storeId.HasValue ? await GetCutoffAsync(storeId.Value, ct) : 0;
        var today = BusinessDayCalculator.TodayBusinessDate(cutoff);
        var (todayStart, _) = BusinessDayCalculator.RangeOf(today, cutoff);
        var (monthStart, _) = BusinessDayCalculator.MonthRangeOf(today.Year, today.Month, cutoff);

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

    /// <summary>月报：本月每天的订单数 / 营业额 / 钟数；汇总营业额、充值、客单价。</summary>
    [HttpGet("monthly")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<MonthlyReportDto>> Monthly(
        [FromQuery] long storeId,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        CancellationToken ct = default)
    {
        var cutoff = await GetCutoffAsync(storeId, ct);
        var today = BusinessDayCalculator.TodayBusinessDate(cutoff);
        var y = year ?? today.Year;
        var m = month ?? today.Month;
        if (m < 1 || m > 12) return BadRequest(new { code = "InvalidMonth", message = "月份不合法" });

        var (start, end) = BusinessDayCalculator.MonthRangeOf(y, m, cutoff);

        var completed = _db.Orders.AsNoTracking()
            .Where(o => o.StoreId == storeId && o.Status == OrderStatus.Completed
                        && o.CompletedAt >= start && o.CompletedAt < end);

        // 按业务日分桶：先取出最小投影，再在 C# 端用切日计算分组
        var orderRows = await completed
            .Select(o => new { o.CompletedAt, o.PaidAmount, Rounds = o.Items.Sum(i => i.Quantity) })
            .ToListAsync(ct);
        var daily = orderRows
            .GroupBy(r => BusinessDayCalculator.BusinessDateOf(r.CompletedAt!.Value, cutoff))
            .Select(g => new MonthlyReportPointDto(
                g.Key.ToDateTime(TimeOnly.MinValue),
                g.Count(),
                g.Sum(r => r.PaidAmount),
                g.Sum(r => r.Rounds)))
            .OrderBy(p => p.Day)
            .ToList();

        // 计时房已结算收入按结算业务日并入（已挂订单的 session 跳过，避免与 Orders.PaidAmount 重算）
        var timedRows = await _db.TimedRoomSessions.AsNoTracking()
            .Where(s => s.StoreId == storeId && s.Status == TimedRoomSessionStatus.Settled
                        && s.OrderId == null
                        && s.EndedAt != null && s.EndedAt >= start && s.EndedAt < end)
            .Select(s => new { s.EndedAt, s.Amount })
            .ToListAsync(ct);
        var timedByDay = timedRows
            .GroupBy(t => BusinessDayCalculator.BusinessDateOf(t.EndedAt!.Value, cutoff))
            .Select(g => new { Day = g.Key.ToDateTime(TimeOnly.MinValue), Amount = g.Sum(x => x.Amount) })
            .ToList();
        if (timedByDay.Count > 0)
        {
            var map = daily.ToDictionary(p => p.Day);
            foreach (var t in timedByDay)
            {
                map[t.Day] = map.TryGetValue(t.Day, out var ex)
                    ? ex with { Revenue = ex.Revenue + t.Amount }
                    : new MonthlyReportPointDto(t.Day, 0, t.Amount, 0);
            }
            daily = map.Values.OrderBy(p => p.Day).ToList();
        }

        var orderCount = daily.Sum(p => p.OrderCount);
        var revenue = daily.Sum(p => p.Revenue);
        var rounds = daily.Sum(p => p.Rounds);
        var recharge = await _db.MemberRechargeRecords.AsNoTracking()
            .Where(r => r.StoreId == storeId && r.CreatedAt >= start && r.CreatedAt < end)
            .SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;
        var avg = orderCount > 0 ? Math.Round(revenue / orderCount, 2) : 0m;

        return Ok(new MonthlyReportDto(y, m, storeId, orderCount, revenue, recharge, rounds, avg, daily));
    }

    /// <summary>年报：按月聚合营业额 / 订单数 / 钟数。</summary>
    [HttpGet("yearly")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<YearlyReportDto>> Yearly(
        [FromQuery] long storeId,
        [FromQuery] int? year = null,
        CancellationToken ct = default)
    {
        var y = year ?? DateTime.UtcNow.Year;
        var start = new DateTime(y, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddYears(1);

        // EF Core 8 + Pomelo 不能翻译 GroupBy 内嵌 SelectMany；先扁平化拉到内存再 C# 端 group
        var orderRows = await _db.Orders.AsNoTracking()
            .Where(o => o.StoreId == storeId && o.Status == OrderStatus.Completed
                        && o.CompletedAt >= start && o.CompletedAt < end)
            .Select(o => new { o.CompletedAt, o.PaidAmount, Rounds = o.Items.Sum(i => i.Quantity) })
            .ToListAsync(ct);
        var monthly = orderRows
            .GroupBy(r => new { r.CompletedAt!.Value.Year, r.CompletedAt!.Value.Month })
            .Select(g => new MonthlyReportPointDto(
                new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                g.Count(),
                g.Sum(r => r.PaidAmount),
                g.Sum(r => r.Rounds)))
            .OrderBy(p => p.Day)
            .ToList();

        // 计时房已结算收入按结算月并入（已挂订单的 session 跳过，避免与 Orders.PaidAmount 重算）
        var timedRows = await _db.TimedRoomSessions.AsNoTracking()
            .Where(s => s.StoreId == storeId && s.Status == TimedRoomSessionStatus.Settled
                        && s.OrderId == null
                        && s.EndedAt != null && s.EndedAt >= start && s.EndedAt < end)
            .Select(s => new { s.EndedAt, s.Amount })
            .ToListAsync(ct);
        var timedByMonth = timedRows
            .GroupBy(t => new { t.EndedAt!.Value.Year, t.EndedAt!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(t => t.Amount) })
            .ToList();
        if (timedByMonth.Count > 0)
        {
            var map = monthly.ToDictionary(p => p.Day);
            foreach (var t in timedByMonth)
            {
                var key = new DateTime(t.Year, t.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                map[key] = map.TryGetValue(key, out var ex)
                    ? ex with { Revenue = ex.Revenue + t.Amount }
                    : new MonthlyReportPointDto(key, 0, t.Amount, 0);
            }
            monthly = map.Values.OrderBy(p => p.Day).ToList();
        }

        return Ok(new YearlyReportDto(
            y, storeId,
            monthly.Sum(p => p.OrderCount),
            monthly.Sum(p => p.Revenue),
            monthly.Sum(p => p.Rounds),
            monthly));
    }

    /// <summary>服务热度：指定区间内各服务项的下单次数 / 钟数 / 营业额。</summary>
    [HttpGet("service-popularity")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<IReadOnlyList<ServicePopularityDto>>> ServicePopularity(
        [FromQuery] long storeId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct = default)
    {
        if (to <= from) return BadRequest(new { code = "InvalidRange", message = "结束时间必须大于开始时间" });

        var rows = await _db.OrderItems.AsNoTracking()
            .Where(oi => oi.Order.StoreId == storeId
                         && oi.Order.Status == OrderStatus.Completed
                         && oi.Order.CompletedAt >= from
                         && oi.Order.CompletedAt < to)
            .Select(oi => new { oi.ServiceId, oi.ServiceName, oi.Quantity, oi.ItemTotal })
            .ToListAsync(ct);
        var data = rows
            .GroupBy(r => new { r.ServiceId, r.ServiceName })
            .Select(g => new ServicePopularityDto(
                g.Key.ServiceId, g.Key.ServiceName,
                g.Count(),
                g.Sum(x => x.Quantity),
                g.Sum(x => x.ItemTotal)))
            .OrderByDescending(s => s.RoundsCount)
            .ToList();
        return Ok(data);
    }

    /// <summary>客流：指定区间内每天的订单数 / 唯一会员数（含匿名按 0 计为同一桶）。</summary>
    [HttpGet("customer-flow")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<IReadOnlyList<CustomerFlowPointDto>>> CustomerFlow(
        [FromQuery] long storeId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct = default)
    {
        if (to <= from) return BadRequest(new { code = "InvalidRange", message = "结束时间必须大于开始时间" });

        var cutoff = await GetCutoffAsync(storeId, ct);
        var rows = await _db.Orders.AsNoTracking()
            .Where(o => o.StoreId == storeId
                        && o.Status == OrderStatus.Completed
                        && o.CompletedAt >= from
                        && o.CompletedAt < to)
            .Select(o => new { o.CompletedAt, o.MemberId })
            .ToListAsync(ct);
        var data = rows
            .GroupBy(r => BusinessDayCalculator.BusinessDateOf(r.CompletedAt!.Value, cutoff))
            .Select(g => new CustomerFlowPointDto(
                g.Key.ToDateTime(TimeOnly.MinValue),
                g.Count(),
                g.Select(r => r.MemberId ?? 0L).Distinct().Count()))
            .OrderBy(p => p.Date)
            .ToList();
        return Ok(data);
    }

    /// <summary>会员分析：按最近消费时间分活跃(≤30天)/沉睡(31-90天)/流失(>90天)，含本月新增与复购率。</summary>
    [HttpGet("member-analysis")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<MemberAnalysisDto>> MemberAnalysis(
        [FromQuery] long storeId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var d30 = now.AddDays(-30);
        var d90 = now.AddDays(-90);

        var members = _db.Members.AsNoTracking().Where(m => m.StoreId == storeId);
        var total = await members.CountAsync(ct);
        var newThisMonth = await members.CountAsync(m => m.CreatedAt >= monthStart, ct);

        var storeMemberIds = members.Select(m => m.Id);
        var consumption = await _db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed
                        && o.MemberId != null && o.CompletedAt != null
                        && storeMemberIds.Contains(o.MemberId!.Value))
            .GroupBy(o => o.MemberId!.Value)
            .Select(g => new { LastAt = g.Max(o => o.CompletedAt!.Value), Count = g.Count() })
            .ToListAsync(ct);

        var consumed = consumption.Count;
        var active = consumption.Count(c => c.LastAt >= d30);
        var dormant = consumption.Count(c => c.LastAt < d30 && c.LastAt >= d90);
        var lost = consumption.Count(c => c.LastAt < d90);
        var repeat = consumption.Count(c => c.Count >= 2);
        var repeatRate = consumed > 0 ? Math.Round((decimal)repeat / consumed * 100m, 1) : 0m;

        return Ok(new MemberAnalysisDto(
            storeId, total, total - consumed,
            active, dormant, lost,
            newThisMonth, repeat, repeatRate));
    }

    /// <summary>服务热度趋势：近 N 个月最热门服务（按总钟数取前 8）的逐月钟数。</summary>
    [HttpGet("service-trend")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<ServicePopularityTrendDto>> ServiceTrend(
        [FromQuery] long storeId, [FromQuery] int months = 6, CancellationToken ct = default)
    {
        months = Math.Clamp(months, 1, 24);
        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var windowStart = currentMonthStart.AddMonths(-(months - 1));

        var rows = await _db.OrderItems.AsNoTracking()
            .Where(oi => oi.Order.StoreId == storeId
                         && oi.Order.Status == OrderStatus.Completed
                         && oi.Order.CompletedAt != null && oi.Order.CompletedAt >= windowStart)
            .Select(oi => new
            {
                oi.ServiceId,
                oi.ServiceName,
                oi.Quantity,
                Completed = oi.Order.CompletedAt!.Value
            })
            .ToListAsync(ct);

        const int topN = 8;
        var services = rows
            .GroupBy(r => new { r.ServiceId, r.ServiceName })
            .Select(g => new { g.Key.ServiceId, g.Key.ServiceName, Total = g.Sum(x => x.Quantity), Rows = g.ToList() })
            .OrderByDescending(s => s.Total)
            .Take(topN)
            .Select(s => new ServiceTrendDto(
                s.ServiceId, s.ServiceName, s.Total,
                Enumerable.Range(0, months).Select(i =>
                {
                    var m = windowStart.AddMonths(i);
                    var rounds = s.Rows
                        .Where(r => r.Completed.Year == m.Year && r.Completed.Month == m.Month)
                        .Sum(r => r.Quantity);
                    return new ServiceTrendMonthDto(m.Year, m.Month, rounds);
                }).ToList()))
            .ToList();

        return Ok(new ServicePopularityTrendDto(months, services));
    }

    /// <summary>技师质量：指定区间内各技师的钟数、有效投诉数与投诉率（已撤销投诉不计）。</summary>
    [HttpGet("technician-quality")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<IReadOnlyList<TechnicianQualityDto>>> TechnicianQuality(
        [FromQuery] long storeId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct = default)
    {
        if (to <= from) return BadRequest(new { code = "InvalidRange", message = "结束时间必须大于开始时间" });

        var roundRows = await _db.OrderItems.AsNoTracking()
            .Where(oi => oi.Order.StoreId == storeId
                         && oi.Order.Status == OrderStatus.Completed
                         && oi.Order.CompletedAt >= from && oi.Order.CompletedAt < to)
            .Select(oi => new
            {
                oi.TechnicianId,
                oi.Technician.RealName,
                oi.Technician.Username,
                oi.Technician.EmployeeNo,
                oi.Quantity
            })
            .ToListAsync(ct);
        var rounds = roundRows
            .GroupBy(r => new { r.TechnicianId, r.RealName, r.Username, r.EmployeeNo })
            .Select(g => new
            {
                g.Key.TechnicianId,
                Name = g.Key.RealName ?? g.Key.Username,
                g.Key.EmployeeNo,
                Rounds = g.Sum(x => x.Quantity)
            })
            .ToList();

        // 匿名投诉 (OriginalTechnicianId == null) 不计入任何技师；只统计有具体技师的投诉
        var complaintMap = (await _db.ServiceComplaints.AsNoTracking()
            .Where(c => c.StoreId == storeId && c.Status != ComplaintStatus.Cancelled
                        && c.OriginalTechnicianId != null
                        && c.CreatedAt >= from && c.CreatedAt < to)
            .GroupBy(c => c.OriginalTechnicianId!.Value)
            .Select(g => new { TechnicianId = g.Key, Count = g.Count() })
            .ToListAsync(ct))
            .ToDictionary(x => x.TechnicianId, x => x.Count);

        var result = rounds
            .Select(r =>
            {
                var cc = complaintMap.TryGetValue(r.TechnicianId, out var c) ? c : 0;
                var rate = r.Rounds > 0 ? Math.Round((decimal)cc / r.Rounds * 100m, 2) : 0m;
                return new TechnicianQualityDto(r.TechnicianId, r.Name, r.EmployeeNo, r.Rounds, cc, rate);
            })
            .OrderByDescending(d => d.ComplaintRate)
            .ThenByDescending(d => d.ComplaintCount)
            .ToList();

        return Ok(result);
    }

    // ==================== 收益导出（Excel） ====================
    // 两个端点都支持三种周期口径：mode=year（按年，走切日月窗合并）/ month（按月，走切日月窗）
    // / range（自定义日期区间，用前端给的 from/to 开区间，与现有区间报表一致）。

    private const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static DateTime ToBeijing(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(8);

    private static string PayMethodLabel(PayMethod m) => m switch
    {
        PayMethod.Cash => "现金",
        PayMethod.MemberCard => "会员卡",
        PayMethod.Wechat => "微信",
        PayMethod.Alipay => "支付宝",
        PayMethod.BankCard => "银行卡",
        PayMethod.Other => "其他",
        PayMethod.Unpaid => "未支付",
        _ => m.ToString()
    };

    /// <summary>把 mode/year/month/from/to 解析为 [start,end) UTC 窗口、中文周期标签与分桶粒度。</summary>
    private static (DateTime Start, DateTime End, string Label, bool MonthlyBuckets) ResolveWindow(
        string mode, int? year, int? month, DateTime? from, DateTime? to, int cutoff)
    {
        switch ((mode ?? "month").Trim().ToLowerInvariant())
        {
            case "year":
            {
                var y = year ?? BusinessDayCalculator.TodayBusinessDate(cutoff).Year;
                var (s, _) = BusinessDayCalculator.MonthRangeOf(y, 1, cutoff);
                var (_, e) = BusinessDayCalculator.MonthRangeOf(y, 12, cutoff);
                return (s, e, $"{y}年", true);
            }
            case "range":
            {
                if (from is null || to is null)
                    throw new ArgumentException("range 模式需要 from 与 to");
                var s = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
                var e = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
                if (e <= s) throw new ArgumentException("结束时间必须大于开始时间");
                var fromLabel = ToBeijing(s).ToString("yyyy-MM-dd");
                var toLabel = ToBeijing(e.AddSeconds(-1)).ToString("yyyy-MM-dd");
                return (s, e, $"{fromLabel} ~ {toLabel}", false);
            }
            default:
            {
                var today = BusinessDayCalculator.TodayBusinessDate(cutoff);
                var y = year ?? today.Year;
                var m = month ?? today.Month;
                if (m < 1 || m > 12) throw new ArgumentException("月份不合法");
                var (s, e) = BusinessDayCalculator.MonthRangeOf(y, m, cutoff);
                return (s, e, $"{y}年{m:D2}月", false);
            }
        }
    }

    [HttpGet("revenue/export")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<IActionResult> ExportRevenueReport(
        [FromQuery] long storeId,
        [FromQuery] string mode = "month",
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var cutoff = await GetCutoffAsync(storeId, ct);
        DateTime start, end; string label; bool monthlyBuckets;
        try { (start, end, label, monthlyBuckets) = ResolveWindow(mode, year, month, from, to, cutoff); }
        catch (ArgumentException ex) { return BadRequest(new { code = "InvalidRange", message = ex.Message }); }

        var storeName = await _db.Stores.AsNoTracking()
            .Where(s => s.Id == storeId).Select(s => s.Name).FirstOrDefaultAsync(ct) ?? $"门店#{storeId}";

        var completed = _db.Orders.AsNoTracking()
            .Where(o => o.StoreId == storeId && o.Status == OrderStatus.Completed
                        && o.CompletedAt >= start && o.CompletedAt < end);
        var timed = _db.TimedRoomSessions.AsNoTracking()
            .Where(s => s.StoreId == storeId && s.Status == TimedRoomSessionStatus.Settled
                        && s.OrderId == null && s.EndedAt != null
                        && s.EndedAt >= start && s.EndedAt < end);

        // 按支付方式（订单 + 计时房合并）
        var orderPay = await completed.GroupBy(o => o.PayMethod)
            .Select(g => new { g.Key, Sum = g.Sum(o => o.PaidAmount) }).ToListAsync(ct);
        var timedPay = await timed.GroupBy(s => s.PayMethod)
            .Select(g => new { g.Key, Sum = g.Sum(s => s.Amount) }).ToListAsync(ct);
        var payAgg = new Dictionary<PayMethod, decimal>();
        foreach (var p in orderPay) payAgg[p.Key] = payAgg.GetValueOrDefault(p.Key) + p.Sum;
        foreach (var p in timedPay) payAgg[p.Key] = payAgg.GetValueOrDefault(p.Key) + p.Sum;
        var payRows = new[]
            {
                PayMethod.Cash, PayMethod.MemberCard, PayMethod.Wechat,
                PayMethod.Alipay, PayMethod.BankCard, PayMethod.Other
            }
            .Select(m => new RevenuePayRow(PayMethodLabel(m), payAgg.GetValueOrDefault(m)))
            .ToList();

        var orderRevenue = await completed.SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;
        var timedRevenue = await timed.SumAsync(s => (decimal?)s.Amount, ct) ?? 0m;
        var revenue = orderRevenue + timedRevenue;
        var orderCount = await completed.CountAsync(ct);
        var rounds = await _db.OrderItems.AsNoTracking()
            .Where(oi => oi.Order.StoreId == storeId && oi.Order.Status == OrderStatus.Completed
                         && oi.Order.CompletedAt >= start && oi.Order.CompletedAt < end)
            .SumAsync(oi => (int?)oi.Quantity, ct) ?? 0;

        var refunded = _db.Orders.AsNoTracking()
            .Where(o => o.StoreId == storeId && o.Status == OrderStatus.Refunded
                        && o.CompletedAt >= start && o.CompletedAt < end);
        var refundCount = await refunded.CountAsync(ct);
        var refundAmount = await refunded.SumAsync(o => (decimal?)o.PaidAmount, ct) ?? 0m;

        var recharges = _db.MemberRechargeRecords.AsNoTracking()
            .Where(rr => rr.StoreId == storeId && rr.CreatedAt >= start && rr.CreatedAt < end);
        var rechargeCount = await recharges.CountAsync(ct);
        var rechargeAmount = await recharges.SumAsync(rr => (decimal?)rr.Amount, ct) ?? 0m;

        // 分期明细：按日（month/range）或按月（year）
        var orderBucketRows = await completed
            .Select(o => new { o.CompletedAt, o.PaidAmount, Rounds = o.Items.Sum(i => i.Quantity) })
            .ToListAsync(ct);
        var timedBucketRows = await timed
            .Select(s => new { s.EndedAt, s.Amount }).ToListAsync(ct);

        string KeyOf(DateTime utc) => monthlyBuckets
            ? ToBeijing(utc).ToString("yyyy-MM")
            : BusinessDayCalculator.BusinessDateOf(utc, cutoff).ToString("yyyy-MM-dd");

        var buckets = new SortedDictionary<string, (int Count, decimal Rev, int Rounds)>();
        foreach (var o in orderBucketRows)
        {
            var k = KeyOf(o.CompletedAt!.Value);
            var cur = buckets.GetValueOrDefault(k);
            buckets[k] = (cur.Count + 1, cur.Rev + o.PaidAmount, cur.Rounds + o.Rounds);
        }
        foreach (var t in timedBucketRows)
        {
            var k = KeyOf(t.EndedAt!.Value);
            var cur = buckets.GetValueOrDefault(k);
            buckets[k] = (cur.Count, cur.Rev + t.Amount, cur.Rounds);
        }
        var periods = buckets
            .Select(b => new RevenuePeriodRow(b.Key, b.Value.Count, b.Value.Rev, b.Value.Rounds))
            .ToList();

        var data = new RevenueReportData(
            storeName, label, DateTime.UtcNow.AddHours(8),
            revenue, orderCount, rounds,
            refundCount, refundAmount,
            rechargeCount, rechargeAmount,
            payRows,
            monthlyBuckets ? "月份" : "日期",
            periods);

        var bytes = ReportExcelBuilder.BuildRevenueReport(data);
        return File(bytes, XlsxContentType, $"收益报表_{storeName}_{label}.xlsx");
    }

    [HttpGet("revenue-detail/export")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<IActionResult> ExportRevenueDetail(
        [FromQuery] long storeId,
        [FromQuery] string mode = "month",
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var cutoff = await GetCutoffAsync(storeId, ct);
        DateTime start, end; string label;
        try { (start, end, label, _) = ResolveWindow(mode, year, month, from, to, cutoff); }
        catch (ArgumentException ex) { return BadRequest(new { code = "InvalidRange", message = ex.Message }); }

        var storeName = await _db.Stores.AsNoTracking()
            .Where(s => s.Id == storeId).Select(s => s.Name).FirstOrDefaultAsync(ct) ?? $"门店#{storeId}";

        // 已完成服务订单
        var orderRows = await _db.Orders.AsNoTracking()
            .Where(o => o.StoreId == storeId && o.Status == OrderStatus.Completed
                        && o.CompletedAt >= start && o.CompletedAt < end)
            .Select(o => new
            {
                o.CompletedAt,
                o.OrderNo,
                MemberName = o.Member != null ? o.Member.Name : null,
                MemberPhone = o.Member != null ? o.Member.Phone : null,
                o.PaidAmount,
                o.PayMethod,
                Cashier = o.CashierUser != null ? (o.CashierUser.RealName ?? o.CashierUser.Username) : null,
                o.Remark,
                Items = o.Items.Select(i => new
                {
                    i.ServiceName,
                    i.Quantity,
                    Tech = i.Technician.RealName ?? i.Technician.Username
                }).ToList()
            })
            .ToListAsync(ct);

        // 计时房独立流水（未挂订单）
        var timedRows = await _db.TimedRoomSessions.AsNoTracking()
            .Where(s => s.StoreId == storeId && s.Status == TimedRoomSessionStatus.Settled
                        && s.OrderId == null && s.EndedAt != null
                        && s.EndedAt >= start && s.EndedAt < end)
            .Select(s => new
            {
                s.EndedAt,
                RoomNo = s.Room.RoomNo,
                MemberName = s.Member != null ? s.Member.Name : null,
                MemberPhone = s.Member != null ? s.Member.Phone : null,
                s.CustomerName,
                s.Amount,
                s.PayMethod,
                s.BilledMinutes,
                Operator = s.OperatorUser != null ? (s.OperatorUser.RealName ?? s.OperatorUser.Username) : null,
                s.Remark
            })
            .ToListAsync(ct);

        var rows = new List<RevenueDetailRow>();
        foreach (var o in orderRows)
        {
            var items = string.Join("；", o.Items.Select(i => $"{i.ServiceName}×{i.Quantity}"));
            var techs = string.Join("、", o.Items.Select(i => i.Tech).Where(t => t != null).Distinct());
            rows.Add(new RevenueDetailRow(
                ToBeijing(o.CompletedAt!.Value),
                "服务订单",
                o.OrderNo,
                o.MemberName ?? "散客",
                o.MemberPhone,
                items,
                techs,
                o.Items.Sum(i => i.Quantity),
                o.PaidAmount,
                PayMethodLabel(o.PayMethod),
                o.Cashier ?? "",
                o.Remark));
        }
        foreach (var t in timedRows)
        {
            rows.Add(new RevenueDetailRow(
                ToBeijing(t.EndedAt!.Value),
                "计时房",
                $"房间 {t.RoomNo}",
                t.MemberName ?? t.CustomerName ?? "散客",
                t.MemberPhone,
                $"计时房费（{t.BilledMinutes}分钟）",
                "",
                0,
                t.Amount,
                PayMethodLabel(t.PayMethod),
                t.Operator ?? "",
                t.Remark));
        }
        rows = rows.OrderBy(r => r.Time).ToList();

        var data = new RevenueDetailData(storeName, label, DateTime.UtcNow.AddHours(8), rows);
        var bytes = ReportExcelBuilder.BuildRevenueDetail(data);
        return File(bytes, XlsxContentType, $"收益明细_{storeName}_{label}.xlsx");
    }
}
