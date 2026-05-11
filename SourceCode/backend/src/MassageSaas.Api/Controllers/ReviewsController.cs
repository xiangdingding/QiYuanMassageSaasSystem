using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
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
            // 匿名提交：通过 openId 必须匹配同店同时间段的预约
            if (string.IsNullOrWhiteSpace(openId))
                return Forbid();
            var matched = await _db.Appointments.AnyAsync(a =>
                a.StoreId == item.Order.StoreId
                && a.CustomerOpenId == openId
                && a.Status == AppointmentStatus.Completed
                && a.ArrivedAt != null
                && a.ArrivedAt >= item.Order.CreatedAt.AddHours(-12)
                && a.ArrivedAt <= item.Order.CreatedAt.AddHours(12), ct);
            if (!matched) return Forbid();
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
    public async Task<ActionResult<IReadOnlyList<ServiceReviewDto>>> List(
        [FromQuery] long? technicianId = null,
        [FromQuery] int? rating = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
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

        var rows = await q.OrderByDescending(r => r.CreatedAt).Take(200).ToListAsync(ct);
        return Ok(rows.Select(MapDto).ToList());
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

        var data = await q
            .GroupBy(r => new { r.TechnicianId, Name = r.Technician.RealName ?? r.Technician.Username })
            .Select(g => new TechnicianReviewSummaryDto(
                g.Key.TechnicianId,
                g.Key.Name,
                g.Count(),
                Math.Round(g.Average(x => (decimal)x.Rating), 2)))
            .OrderByDescending(x => x.AverageRating)
            .ToListAsync(ct);
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
