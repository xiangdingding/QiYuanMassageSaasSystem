using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Reviews;
using MassageSaas.Shared.Storefront;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

/// <summary>
/// 顾客小程序的门店前台只读接口：服务菜单、技师列表、会员卡概要。
/// 全部匿名——顾客没有店员账号。通过 storeId 反查租户、用 openId 反查会员，全程 BypassTenantFilter。
/// </summary>
[ApiController]
[Route("api/storefront")]
[AllowAnonymous]
public class StorefrontController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public StorefrontController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <summary>门店服务菜单（仅启用项）。</summary>
    [HttpGet("services")]
    public async Task<ActionResult<IReadOnlyList<StorefrontServiceDto>>> Services(
        [FromQuery] long storeId, CancellationToken ct)
    {
        _tenantContext.BypassTenantFilter();
        var tenantId = await ResolveTenantAsync(storeId, ct);
        if (tenantId is null)
            return BadRequest(new { code = "StoreNotFound", message = "门店不存在或已停用" });

        var rows = await _db.ServiceItems.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new StorefrontServiceDto(
                s.Id, s.Name, s.DurationMinutes, s.Price, s.MemberPrice, s.Description))
            .ToListAsync(ct);
        return Ok(rows);
    }

    /// <summary>门店在岗技师（供预约时指定）。</summary>
    [HttpGet("technicians")]
    public async Task<ActionResult<IReadOnlyList<StorefrontTechnicianDto>>> Technicians(
        [FromQuery] long storeId, CancellationToken ct)
    {
        _tenantContext.BypassTenantFilter();
        var tenantId = await ResolveTenantAsync(storeId, ct);
        if (tenantId is null)
            return BadRequest(new { code = "StoreNotFound", message = "门店不存在或已停用" });

        var rows = await _db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.StoreId == storeId
                        && u.Role == UserRole.Technician && u.IsActive)
            .OrderBy(u => u.RealName)
            .Select(u => new StorefrontTechnicianDto(
                u.Id,
                u.RealName ?? u.Username,
                u.TechnicianLevel.ToString(),
                u.IsBlind,
                u.Specialties))
            .ToListAsync(ct);
        return Ok(rows);
    }

    /// <summary>按微信 OpenId 查会员卡实时概要与在用套餐。未绑定返回 404。</summary>
    [HttpGet("member")]
    public async Task<ActionResult<StorefrontMemberDto>> Member(
        [FromQuery] string? openId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(openId))
            return BadRequest(new { code = "InvalidInput", message = "缺少 openId" });

        _tenantContext.BypassTenantFilter();
        var member = await _db.Members.AsNoTracking()
            .Where(m => m.WechatOpenId == openId && m.IsActive)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (member is null)
            return NotFound(new { code = "MemberNotFound", message = "该微信未绑定会员卡" });

        var packages = await _db.MemberPackages.AsNoTracking()
            .Include(p => p.Service)
            .Where(p => p.MemberId == member.Id && p.Status == MemberPackageStatus.Active)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new StorefrontPackageDto(
                p.Id, p.Title, p.Kind.ToString(),
                p.Service != null ? p.Service.Name : null,
                p.RemainCount, p.TotalCount, p.ExpiresAt, p.Status.ToString()))
            .ToListAsync(ct);

        return Ok(new StorefrontMemberDto(
            member.Id, member.CardNo, member.Name, member.Balance,
            member.Level.ToString(), packages));
    }

    /// <summary>
    /// 顾客"待评价"列表：按 openId 找到名下所有会员卡，返回这些卡消费过、
    /// 已完成、且尚未评价的订单项（精确到技师 + 服务）。未绑卡返回空列表。
    /// 只暴露顾客自己会员卡的记录，不会泄露他人消费。
    /// </summary>
    [HttpGet("reviewable")]
    public async Task<ActionResult<IReadOnlyList<ReviewableItemDto>>> Reviewable(
        [FromQuery] string? openId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(openId))
            return BadRequest(new { code = "InvalidInput", message = "缺少 openId" });

        _tenantContext.BypassTenantFilter();

        // 一个微信可能在多家门店绑过卡，取其名下全部会员卡 id
        var memberIds = await _db.Members.AsNoTracking()
            .Where(m => m.WechatOpenId == openId)
            .Select(m => m.Id)
            .ToListAsync(ct);
        if (memberIds.Count == 0)
            return Ok(Array.Empty<ReviewableItemDto>());

        var rows = await _db.OrderItems.AsNoTracking()
            .Where(oi => oi.Order.MemberId != null
                         && memberIds.Contains(oi.Order.MemberId.Value)
                         && oi.Order.Status == OrderStatus.Completed
                         && !_db.ServiceReviews.Any(r => r.OrderItemId == oi.Id))
            .OrderByDescending(oi => oi.Order.CompletedAt)
            .Take(100)
            .Select(oi => new ReviewableItemDto(
                oi.OrderId,
                oi.Id,
                oi.Order.OrderNo,
                oi.TechnicianId,
                oi.Technician != null ? (oi.Technician.RealName ?? oi.Technician.Username) : string.Empty,
                oi.ServiceName,
                oi.Order.CompletedAt))
            .ToListAsync(ct);
        return Ok(rows);
    }

    private async Task<long?> ResolveTenantAsync(long storeId, CancellationToken ct)
    {
        var store = await _db.Stores.AsNoTracking()
            .Where(s => s.Id == storeId && s.IsActive)
            .Select(s => new { s.TenantId })
            .FirstOrDefaultAsync(ct);
        return store?.TenantId;
    }
}
