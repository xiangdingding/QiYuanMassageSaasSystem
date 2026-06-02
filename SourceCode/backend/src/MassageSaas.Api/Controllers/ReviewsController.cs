using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Common;
using MassageSaas.Shared.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ReviewsController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// 顾客在小程序提交评价。允许匿名（通过 openId 自证 = 该订单是这个 openId 下的预约），
    /// 也允许店员代录入。
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceReviewDto>> Submit(
        [FromBody] SubmitReviewRequest req,
        [FromQuery] string? openId,
        CancellationToken ct)
    {
        if (req.Rating < 1 || req.Rating > 5)
            return BadRequest(new { code = "InvalidRating", message = "评分必须在 1-5" });

        _tenantContext.BypassTenantFilter();

        var item = await _db.OrderItems
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.Id == req.OrderItemId && i.OrderId == req.OrderId, ct);
        if (item is null) return NotFound(new { code = "ItemNotFound", message = "订单项不存在" });
        if (item.Order.Status != OrderStatus.Completed)
            return Conflict(new { code = "NotCompleted", message = "订单未完成不能评价" });

        var already = await _db.ServiceReviews.AnyAsync(r => r.OrderItemId == req.OrderItemId, ct);
        if (already) return Conflict(new { code = "AlreadyReviewed", message = "该服务已评价" });

        var isStaff = User.Identity?.IsAuthenticated == true;
        if (!isStaff)
        {
            // 匿名提交必须带 openId，且满足以下任一自证路径：
            if (string.IsNullOrWhiteSpace(openId))
                return Forbid();
            // 路径一：openId 已绑定到该订单的会员卡（小程序"我的-待评价"主流程）
            var byMember = item.Order.MemberId.HasValue && await _db.Members.AnyAsync(
                m => m.Id == item.Order.MemberId.Value && m.WechatOpenId == openId, ct);
            // 路径二：openId 在同店有时间相近（±12h）的已完成预约（覆盖未绑卡但预约过的顾客）
            var byAppointment = !byMember && await _db.Appointments.AnyAsync(a =>
                a.StoreId == item.Order.StoreId
                && a.CustomerOpenId == openId
                && a.Status == AppointmentStatus.Completed
                && a.ArrivedAt != null
                && a.ArrivedAt >= item.Order.CreatedAt.AddHours(-12)
                && a.ArrivedAt <= item.Order.CreatedAt.AddHours(12), ct);
            if (!byMember && !byAppointment) return Forbid();
        }

        var review = new ServiceReview
        {
            TenantId = item.TenantId,
            OrderId = req.OrderId,
            OrderItemId = req.OrderItemId,
            TechnicianId = item.TechnicianId,
            MemberId = item.Order.MemberId,
            Rating = req.Rating,
            Tags = req.Tags,
            Comment = req.Comment
        };
        _db.ServiceReviews.Add(review);
        await _db.SaveChangesAsync(ct);

        var saved = await _db.ServiceReviews.AsNoTracking()
            .Include(r => r.Technician)
            .Include(r => r.Member)
            .FirstAsync(r => r.Id == review.Id, ct);
        return Ok(MapDto(saved));
    }

    [HttpGet]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<PagedResult<ServiceReviewDto>>> List(
        [FromQuery] long? technicianId = null,
        [FromQuery] int? rating = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var q = _db.ServiceReviews.AsNoTracking()
            .Include(r => r.Technician)
            .Include(r => r.Member)
            .AsQueryable();
        if (technicianId.HasValue) q = q.Where(r => r.TechnicianId == technicianId.Value);
        if (rating.HasValue) q = q.Where(r => r.Rating == rating.Value);
        if (from.HasValue) q = q.Where(r => r.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(r => r.CreatedAt < to.Value);

        var pq = new PageQuery(page, pageSize, null);
        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pq.SafePage - 1) * pq.SafePageSize)
            .Take(pq.SafePageSize)
            .ToListAsync(ct);
        return Ok(new PagedResult<ServiceReviewDto>(rows.Select(MapDto).ToList(), total, pq.SafePage, pq.SafePageSize));
    }

    [HttpGet("technician-summary")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<IReadOnlyList<TechnicianReviewSummaryDto>>> TechnicianSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var q = _db.ServiceReviews.AsNoTracking().AsQueryable();
        if (from.HasValue) q = q.Where(r => r.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(r => r.CreatedAt < to.Value);

        // 仅按 TechnicianId 标量分组（避免按导航属性 coalesce 分组导致 EF 翻译失败 / 500）。
        // 平均分用 double 让 SQL AVG 翻译，名称单独查再拼接，四舍五入在内存里做。
        var grouped = await q
            .GroupBy(r => r.TechnicianId)
            .Select(g => new { TechnicianId = g.Key, Count = g.Count(), Avg = g.Average(x => (double)x.Rating) })
            .ToListAsync(ct);

        var techIds = grouped.Select(x => x.TechnicianId).ToList();
        var names = await _db.Users.AsNoTracking()
            .Where(u => techIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.RealName ?? u.Username })
            .ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        var data = grouped
            .Select(g => new TechnicianReviewSummaryDto(
                g.TechnicianId,
                names.TryGetValue(g.TechnicianId, out var n) ? n : string.Empty,
                g.Count,
                Math.Round((decimal)g.Avg, 2)))
            .OrderByDescending(x => x.AverageRating)
            .ToList();
        return Ok(data);
    }

    /// <summary>当前登录技师查看自己的评价（小程序"我的评价"）。</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ServiceReviewDto>>> Me(CancellationToken ct)
    {
        if (_tenantContext.UserId is not long uid) return Unauthorized();
        var rows = await _db.ServiceReviews.AsNoTracking()
            .Include(r => r.Member)
            .Include(r => r.Technician)
            .Where(r => r.TechnicianId == uid)
            .OrderByDescending(r => r.CreatedAt)
            .Take(100)
            .ToListAsync(ct);
        return Ok(rows.Select(MapDto).ToList());
    }

    private static ServiceReviewDto MapDto(ServiceReview r) => new(
        r.Id, r.OrderId, r.OrderItemId,
        r.TechnicianId, r.Technician?.RealName ?? r.Technician?.Username ?? string.Empty,
        r.MemberId, r.Member?.Name ?? r.Member?.CardNo,
        r.Rating, r.Tags, r.Comment, r.CreatedAt);
}
