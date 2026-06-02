using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/payroll")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(
        ApplicationDbContext db,
        ITenantContext tenantContext,
        ILogger<PayrollController> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    // ---- 薪资配置 ----

    [HttpGet("profiles")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<IReadOnlyList<SalaryProfileDto>>> ListProfiles(
        [FromQuery] long? storeId = null,
        CancellationToken ct = default)
    {
        var q = _db.SalaryProfiles.AsNoTracking()
            .Include(p => p.User)
            .AsQueryable();
        if (storeId.HasValue) q = q.Where(p => p.User.StoreId == storeId.Value);
        var rows = await q.OrderBy(p => p.User.EmployeeNo).ToListAsync(ct);
        return Ok(rows.Select(MapProfile).ToList());
    }

    [HttpGet("profiles/{userId:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<SalaryProfileDto>> GetProfile(long userId, CancellationToken ct)
    {
        var p = await _db.SalaryProfiles.AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (p is null)
        {
            var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, ct);
            if (u is null) return NotFound();
            return Ok(new SalaryProfileDto(u.Id, u.RealName ?? u.Username, 0m, 0m, 0m, 0, null));
        }
        return Ok(MapProfile(p));
    }

    [HttpPut("profiles/{userId:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<SalaryProfileDto>> UpsertProfile(
        long userId,
        [FromBody] UpsertSalaryProfileRequest req,
        CancellationToken ct)
    {
        if (req.BaseMonthly < 0 || req.OvertimeHourRate < 0 || req.AttendanceBonusAmount < 0 || req.RequiredAttendanceDays < 0)
            return BadRequest(new { code = "InvalidInput", message = "数值不能为负" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return NotFound(new { code = "UserNotFound", message = "员工不存在" });

        var p = await _db.SalaryProfiles.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (p is null)
        {
            p = new SalaryProfile { UserId = userId, User = user };
            _db.SalaryProfiles.Add(p);
        }
        p.BaseMonthly = req.BaseMonthly;
        p.OvertimeHourRate = req.OvertimeHourRate;
        p.AttendanceBonusAmount = req.AttendanceBonusAmount;
        p.RequiredAttendanceDays = req.RequiredAttendanceDays;
        p.Remark = req.Remark;
        await _db.SaveChangesAsync(ct);

        await _db.Entry(p).Reference(x => x.User).LoadAsync(ct);
        return Ok(MapProfile(p));
    }

    // ---- 工资单批次 ----

    [HttpGet("periods")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<IReadOnlyList<PayrollPeriodDto>>> ListPeriods(
        [FromQuery] long storeId,
        [FromQuery] int? year = null,
        CancellationToken ct = default)
    {
        var q = _db.PayrollPeriods.AsNoTracking()
            .Include(p => p.OperatorUser)
            .Where(p => p.StoreId == storeId);
        if (year.HasValue) q = q.Where(p => p.Year == year.Value);
        var rows = await q
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Select(p => new PayrollPeriodDto(
                p.Id, p.StoreId, p.Year, p.Month, p.Status.ToString(),
                p.GeneratedAt, p.LockedAt, p.PaidAt,
                p.OperatorUser != null ? (p.OperatorUser.RealName ?? p.OperatorUser.Username) : null,
                p.TotalAmount, p.Items.Count, p.Remark))
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("periods/{id:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<PayrollPeriodDetailDto>> GetPeriod(long id, CancellationToken ct)
    {
        var period = await LoadPeriodAsync(id, ct);
        return period is null ? NotFound() : Ok(period);
    }

    [HttpPost("periods")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<PayrollPeriodDetailDto>> Generate(
        [FromBody] GeneratePayrollRequest req,
        CancellationToken ct)
    {
        if (req.Year < 2000 || req.Year > 2100 || req.Month < 1 || req.Month > 12)
            return BadRequest(new { code = "InvalidPeriod", message = "年月不合法" });

        var dup = await _db.PayrollPeriods.AnyAsync(p =>
            p.StoreId == req.StoreId && p.Year == req.Year && p.Month == req.Month, ct);
        if (dup) return Conflict(new { code = "DuplicatePeriod", message = "该月工资单已生成，请删除草稿后重新生成或直接修改" });

        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == req.StoreId, ct);
        if (store is null) return BadRequest(new { code = "StoreNotFound", message = "门店不存在" });

        // 业务月 = 业务日 1 号的 cutoff 时刻 ~ 次月 1 号 cutoff 时刻
        var (monthStart, monthEnd) = MassageSaas.Domain.Common.BusinessDayCalculator
            .MonthRangeOf(req.Year, req.Month, store.DayCloseCutoffMinutes);
        var dateFrom = new DateOnly(req.Year, req.Month, 1);
        var dateTo = dateFrom.AddMonths(1).AddDays(-1);

        // 该店所有"按摩店员工"（排除平台管理员）。投影到匿名类型，避免 User 实体反过来挂到图里
        var staffList = await _db.Users.AsNoTracking()
            .Where(u => u.StoreId == req.StoreId && u.Role != UserRole.PlatformAdmin && u.IsActive)
            .Select(u => new { u.Id })
            .ToListAsync(ct);
        var staffIds = staffList.Select(u => u.Id).ToList();

        // 当月提成 / 小费 / 钟数（按 OrderItem 的 Order.CompletedAt 归属）
        var perfRows = await _db.OrderItems.AsNoTracking()
            .Where(oi => staffIds.Contains(oi.TechnicianId)
                         && oi.Order.Status == OrderStatus.Completed
                         && oi.Order.CompletedAt >= monthStart
                         && oi.Order.CompletedAt < monthEnd)
            .GroupBy(oi => oi.TechnicianId)
            .Select(g => new
            {
                UserId = g.Key,
                Commission = g.Sum(x => x.CommissionAmount),
                Tips = g.Sum(x => x.TipAmount),
                Rounds = g.Sum(x => x.Quantity)
            })
            .ToDictionaryAsync(x => x.UserId, ct);

        // 当月排班天数：GroupBy 里套 Distinct().Count() EF Core 翻译不了，先取明细再内存按人去重计数
        var scheduleRaw = await _db.StaffSchedules.AsNoTracking()
            .Where(s => staffIds.Contains(s.UserId) && s.WorkDate >= dateFrom && s.WorkDate <= dateTo)
            .Select(s => new { s.UserId, s.WorkDate })
            .ToListAsync(ct);
        var scheduleDays = scheduleRaw
            .GroupBy(s => s.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.WorkDate).Distinct().Count());

        var leaveRows = await _db.LeaveRequests.AsNoTracking()
            .Where(l => staffIds.Contains(l.UserId)
                        && l.Status == LeaveStatus.Approved
                        && l.FromDate <= dateTo && l.ToDate >= dateFrom)
            .ToListAsync(ct);

        // 薪资配置（可能没有 → 当 0）
        var profiles = await _db.SalaryProfiles.AsNoTracking()
            .Where(p => staffIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, ct);

        var period = new PayrollPeriod
        {
            StoreId = req.StoreId,
            Year = req.Year, Month = req.Month,
            Status = PayrollStatus.Draft,
            GeneratedAt = DateTime.UtcNow,
            OperatorUserId = _tenantContext.UserId,
            Remark = req.Remark
        };

        decimal totalNet = 0m;
        foreach (var entry in staffList)
        {
            var uid = entry.Id;
            profiles.TryGetValue(uid, out var profile);
            perfRows.TryGetValue(uid, out var perf);
            scheduleDays.TryGetValue(uid, out var scheduledDays);
            var leaveDays = leaveRows
                .Where(l => l.UserId == uid)
                .Sum(l => OverlapDays(l.FromDate, l.ToDate, dateFrom, dateTo));

            var attendance = ComputeAttendanceBonus(profile, scheduledDays, leaveDays, DateTime.DaysInMonth(req.Year, req.Month));

            var item = new PayrollItem
            {
                UserId = uid,
                BaseSalary = profile?.BaseMonthly ?? 0m,
                CommissionTotal = perf?.Commission ?? 0m,
                TipsTotal = perf?.Tips ?? 0m,
                OvertimeHours = 0m,
                OvertimeAmount = 0m,
                AttendanceBonus = attendance,
                AdjustmentTotal = 0m,
                ServedRoundCount = perf?.Rounds ?? 0,
                ScheduledDays = scheduledDays,
                LeaveDays = leaveDays,
                NetTotal = (profile?.BaseMonthly ?? 0m)
                         + (perf?.Commission ?? 0m)
                         + attendance
            };
            period.Items.Add(item);
            totalNet += item.NetTotal;
        }
        period.TotalAmount = totalNet;

        _db.PayrollPeriods.Add(period);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Payroll generated store={StoreId} {Year}-{Month} items={ItemCount} total={Total}",
            req.StoreId, req.Year, req.Month, period.Items.Count, totalNet);

        var detail = await LoadPeriodAsync(period.Id, ct);
        return Ok(detail);
    }

    [HttpPost("periods/{id:long}/lock")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<PayrollPeriodDto>> Lock(long id, [FromBody] LockPayrollRequest req, CancellationToken ct)
    {
        var p = await _db.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();
        if (p.Status != PayrollStatus.Draft)
            return Conflict(new { code = "InvalidState", message = "仅草稿状态可封盘" });
        p.Status = PayrollStatus.Locked;
        p.LockedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(req?.Remark))
            p.Remark = (p.Remark ?? string.Empty) + " | 封盘备注: " + req.Remark;
        await _db.SaveChangesAsync(ct);
        return Ok(await LoadHeaderAsync(id, ct));
    }

    [HttpPost("periods/{id:long}/mark-paid")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<PayrollPeriodDto>> MarkPaid(long id, CancellationToken ct)
    {
        var p = await _db.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();
        if (p.Status != PayrollStatus.Locked)
            return Conflict(new { code = "InvalidState", message = "请先封盘再标记已发放" });
        p.Status = PayrollStatus.Paid;
        p.PaidAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(await LoadHeaderAsync(id, ct));
    }

    [HttpDelete("periods/{id:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<IActionResult> DeleteDraft(long id, CancellationToken ct)
    {
        var p = await _db.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();
        if (p.Status != PayrollStatus.Draft)
            return Conflict(new { code = "InvalidState", message = "仅草稿可删除" });
        p.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---- 工资单条目调整 ----

    [HttpPatch("items/{id:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<PayrollItemDto>> UpdateItem(long id, [FromBody] UpdatePayrollItemRequest req, CancellationToken ct)
    {
        if (req.OvertimeHours < 0) return BadRequest(new { code = "InvalidInput", message = "加班小时数不能为负" });

        var item = await _db.PayrollItems.Include(i => i.Period).Include(i => i.Adjustments).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (item.Period.Status != PayrollStatus.Draft)
            return Conflict(new { code = "Locked", message = "已封盘工资单不可修改" });

        var profile = await _db.SalaryProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == item.UserId, ct);
        item.OvertimeHours = req.OvertimeHours;
        item.OvertimeAmount = Math.Round(req.OvertimeHours * (profile?.OvertimeHourRate ?? 0m), 2);
        if (req.AttendanceBonusOverride >= 0m)
            item.AttendanceBonus = req.AttendanceBonusOverride;
        item.Remark = req.Remark;
        RecomputeNet(item);

        await UpdatePeriodTotalAsync(item.PeriodId, ct);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(item).Reference(i => i.User).LoadAsync(ct);
        return Ok(MapItem(item));
    }

    [HttpPost("items/{id:long}/adjustments")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<PayrollItemDto>> AddAdjustment(long id, [FromBody] AddAdjustmentRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<PayrollAdjustmentKind>(req.Kind, true, out var kind))
            return BadRequest(new { code = "InvalidKind", message = "调整类型不合法" });
        if (req.Amount <= 0) return BadRequest(new { code = "InvalidAmount", message = "金额必须 > 0" });
        if (string.IsNullOrWhiteSpace(req.Reason)) return BadRequest(new { code = "InvalidReason", message = "请填写原因" });

        var item = await _db.PayrollItems.Include(i => i.Period).Include(i => i.Adjustments).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (item.Period.Status != PayrollStatus.Draft)
            return Conflict(new { code = "Locked", message = "已封盘工资单不可修改" });

        var adj = new PayrollAdjustment
        {
            ItemId = item.Id,
            Kind = kind,
            Amount = req.Amount,
            Reason = req.Reason.Trim(),
            OperatorUserId = _tenantContext.UserId
        };
        item.Adjustments.Add(adj);
        RecomputeNet(item);
        await UpdatePeriodTotalAsync(item.PeriodId, ct);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(item).Reference(i => i.User).LoadAsync(ct);
        foreach (var a in item.Adjustments.Where(a => a.OperatorUser is null && a.OperatorUserId.HasValue))
            await _db.Entry(a).Reference(x => x.OperatorUser!).LoadAsync(ct);

        return Ok(MapItem(item));
    }

    [HttpDelete("items/{itemId:long}/adjustments/{adjId:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<PayrollItemDto>> RemoveAdjustment(long itemId, long adjId, CancellationToken ct)
    {
        var item = await _db.PayrollItems.Include(i => i.Period).Include(i => i.Adjustments).FirstOrDefaultAsync(x => x.Id == itemId, ct);
        if (item is null) return NotFound();
        if (item.Period.Status != PayrollStatus.Draft)
            return Conflict(new { code = "Locked", message = "已封盘工资单不可修改" });

        var adj = item.Adjustments.FirstOrDefault(a => a.Id == adjId);
        if (adj is null) return NotFound(new { code = "AdjustmentNotFound", message = "调整项不存在" });
        item.Adjustments.Remove(adj);
        adj.IsDeleted = true;
        RecomputeNet(item);
        await UpdatePeriodTotalAsync(item.PeriodId, ct);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(item).Reference(i => i.User).LoadAsync(ct);
        return Ok(MapItem(item));
    }

    // ---- 技师自查 ----

    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<PayrollItemDto>>> MyPayroll(
        [FromQuery] int take = 6,
        CancellationToken ct = default)
    {
        if (_tenantContext.UserId is not long uid) return Unauthorized();
        var rows = await _db.PayrollItems.AsNoTracking()
            .Include(i => i.Period)
            .Include(i => i.User)
            .Include(i => i.Adjustments).ThenInclude(a => a.OperatorUser)
            .Where(i => i.UserId == uid
                        && (i.Period.Status == PayrollStatus.Locked || i.Period.Status == PayrollStatus.Paid))
            .OrderByDescending(i => i.Period.Year).ThenByDescending(i => i.Period.Month)
            .Take(Math.Clamp(take, 1, 24))
            .ToListAsync(ct);
        return Ok(rows.Select(MapItem).ToList());
    }

    // ---- helpers ----

    private static void RecomputeNet(PayrollItem item)
    {
        var adjTotal = item.Adjustments.Sum(a => a.Kind == PayrollAdjustmentKind.Bonus ? a.Amount : -a.Amount);
        item.AdjustmentTotal = adjTotal;
        item.NetTotal = item.BaseSalary
                      + item.CommissionTotal
                      + item.OvertimeAmount
                      + item.AttendanceBonus
                      + adjTotal;
    }

    private async Task UpdatePeriodTotalAsync(long periodId, CancellationToken ct)
    {
        var period = await _db.PayrollPeriods.Include(p => p.Items).FirstAsync(p => p.Id == periodId, ct);
        period.TotalAmount = period.Items.Sum(i => i.NetTotal);
    }

    private static decimal ComputeAttendanceBonus(SalaryProfile? profile, int scheduledDays, int leaveDays, int naturalDays)
    {
        if (profile is null) return 0m;
        if (profile.AttendanceBonusAmount <= 0) return 0m;
        // 未设置满勤所需天数（<=0）时，按当月自然天数作为满勤标准
        var required = profile.RequiredAttendanceDays > 0 ? profile.RequiredAttendanceDays : naturalDays;
        var actual = scheduledDays - leaveDays;
        return actual >= required ? profile.AttendanceBonusAmount : 0m;
    }

    private static int OverlapDays(DateOnly fromA, DateOnly toA, DateOnly fromB, DateOnly toB)
    {
        var from = fromA > fromB ? fromA : fromB;
        var to = toA < toB ? toA : toB;
        if (to < from) return 0;
        return to.DayNumber - from.DayNumber + 1;
    }

    private async Task<PayrollPeriodDetailDto?> LoadPeriodAsync(long id, CancellationToken ct)
    {
        var p = await _db.PayrollPeriods.AsNoTracking()
            .Include(x => x.OperatorUser)
            .Include(x => x.Items).ThenInclude(i => i.User)
            .Include(x => x.Items).ThenInclude(i => i.Adjustments).ThenInclude(a => a.OperatorUser)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;
        var header = new PayrollPeriodDto(
            p.Id, p.StoreId, p.Year, p.Month, p.Status.ToString(),
            p.GeneratedAt, p.LockedAt, p.PaidAt,
            p.OperatorUser != null ? (p.OperatorUser.RealName ?? p.OperatorUser.Username) : null,
            p.TotalAmount, p.Items.Count, p.Remark);
        var items = p.Items.OrderBy(i => i.User.EmployeeNo).ThenBy(i => i.UserId).Select(MapItem).ToList();
        return new PayrollPeriodDetailDto(header, items);
    }

    private async Task<PayrollPeriodDto> LoadHeaderAsync(long id, CancellationToken ct)
    {
        var p = await _db.PayrollPeriods.AsNoTracking()
            .Include(x => x.OperatorUser)
            .FirstAsync(x => x.Id == id, ct);
        var count = await _db.PayrollItems.AsNoTracking().CountAsync(i => i.PeriodId == id, ct);
        return new PayrollPeriodDto(
            p.Id, p.StoreId, p.Year, p.Month, p.Status.ToString(),
            p.GeneratedAt, p.LockedAt, p.PaidAt,
            p.OperatorUser != null ? (p.OperatorUser.RealName ?? p.OperatorUser.Username) : null,
            p.TotalAmount, count, p.Remark);
    }

    private static SalaryProfileDto MapProfile(SalaryProfile p) => new(
        p.UserId, p.User?.RealName ?? p.User?.Username ?? string.Empty,
        p.BaseMonthly, p.OvertimeHourRate, p.AttendanceBonusAmount,
        p.RequiredAttendanceDays, p.Remark);

    private static PayrollItemDto MapItem(PayrollItem i) => new(
        i.Id, i.UserId,
        i.User?.RealName ?? i.User?.Username ?? string.Empty,
        i.User?.EmployeeNo,
        i.BaseSalary, i.CommissionTotal, i.TipsTotal,
        i.OvertimeHours, i.OvertimeAmount, i.AttendanceBonus,
        i.AdjustmentTotal, i.NetTotal,
        i.ServedRoundCount, i.ScheduledDays, i.LeaveDays,
        i.Remark,
        i.Adjustments.OrderBy(a => a.CreatedAt).Select(a => new PayrollAdjustmentDto(
            a.Id, a.Kind.ToString(), a.Amount, a.Reason,
            a.OperatorUser != null ? (a.OperatorUser.RealName ?? a.OperatorUser.Username) : null,
            a.CreatedAt)).ToList());
}
