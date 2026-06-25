using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Common;
using MassageSaas.Shared.Consultations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

/// <summary>
/// 官网业务咨询：官网访客匿名提交（电话 + 内容），平台端（PlatformAdmin）查看与处理。
/// 平台级数据，不做租户隔离。匿名 POST 已在 TenantStatusMiddleware 放行（TenantId 为空）。
/// </summary>
[ApiController]
[Route("api/consultations")]
public class BusinessConsultationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public BusinessConsultationsController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <summary>官网访客提交业务咨询（匿名）。</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<object>> Submit([FromBody] CreateConsultationRequest req, CancellationToken ct)
    {
        var phone = req.Phone?.Trim() ?? string.Empty;
        var content = req.Content?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { code = "PhoneRequired", message = "请填写联系电话" });
        if (phone.Length > 32)
            return BadRequest(new { code = "PhoneTooLong", message = "联系电话格式不正确" });
        if (string.IsNullOrWhiteSpace(content))
            return BadRequest(new { code = "ContentRequired", message = "请填写咨询内容" });
        if (content.Length > 2000)
            content = content[..2000];

        var entity = new BusinessConsultation
        {
            ContactName = string.IsNullOrWhiteSpace(req.ContactName) ? null : req.ContactName.Trim(),
            Phone = phone,
            Content = content,
            Source = string.IsNullOrWhiteSpace(req.Source) ? "website" : req.Source.Trim(),
            SubmitIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Status = ConsultationStatus.Pending
        };
        _db.BusinessConsultations.Add(entity);
        await _db.SaveChangesAsync(ct);

        // 匿名回执不回传内部数据，仅告知成功
        return Ok(new { success = true, message = "提交成功，我们会尽快与您联系" });
    }

    /// <summary>平台端分页查看业务咨询，可按状态/关键词过滤。</summary>
    [HttpGet]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<ActionResult<PagedResult<ConsultationDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? keyword = null,
        CancellationToken ct = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safeSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.BusinessConsultations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ConsultationStatus>(status, out var st))
            query = query.Where(x => x.Status == st);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(x => x.Phone.Contains(kw)
                || (x.ContactName != null && x.ContactName.Contains(kw))
                || x.Content.Contains(kw));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.Status == ConsultationStatus.Pending) // 待处理置顶
            .ThenByDescending(x => x.CreatedAt)
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .Select(x => new ConsultationDto(
                x.Id,
                x.ContactName,
                x.Phone,
                x.Content,
                x.Source,
                x.Status.ToString(),
                x.ProcessNote,
                x.ProcessedByUser != null ? (x.ProcessedByUser.RealName ?? x.ProcessedByUser.Username) : null,
                x.ProcessedAt,
                x.CreatedAt))
            .ToListAsync(ct);

        return Ok(new PagedResult<ConsultationDto>(items, total, safePage, safeSize));
    }

    /// <summary>平台端待处理数量（用于菜单红点/角标）。</summary>
    [HttpGet("pending-count")]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<ActionResult<object>> PendingCount(CancellationToken ct)
    {
        var count = await _db.BusinessConsultations
            .CountAsync(x => x.Status == ConsultationStatus.Pending, ct);
        return Ok(new { count });
    }

    /// <summary>平台端处理业务咨询：更新状态 + 备注。</summary>
    [HttpPut("{id:long}/process")]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<ActionResult<ConsultationDto>> Process(
        long id, [FromBody] ProcessConsultationRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<ConsultationStatus>(req.Status, out var status))
            return BadRequest(new { code = "InvalidStatus", message = "无效的处理状态" });

        var entity = await _db.BusinessConsultations
            .Include(x => x.ProcessedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { code = "NotFound", message = "咨询记录不存在" });

        entity.Status = status;
        entity.ProcessNote = string.IsNullOrWhiteSpace(req.ProcessNote) ? null : req.ProcessNote.Trim();
        entity.ProcessedByUserId = _tenantContext.UserId;
        entity.ProcessedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var name = entity.ProcessedByUserId.HasValue
            ? await _db.Users.Where(u => u.Id == entity.ProcessedByUserId)
                .Select(u => u.RealName ?? u.Username).FirstOrDefaultAsync(ct)
            : null;

        return Ok(new ConsultationDto(
            entity.Id, entity.ContactName, entity.Phone, entity.Content, entity.Source,
            entity.Status.ToString(), entity.ProcessNote, name, entity.ProcessedAt, entity.CreatedAt));
    }
}
