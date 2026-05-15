using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Common;
using MassageSaas.Shared.Complaints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/complaints")]
[Authorize(Policy = "ShopStaff")]
public class ComplaintsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ComplaintsController> _logger;

    public ComplaintsController(
        ApplicationDbContext db,
        ITenantContext tenantContext,
        ILogger<ComplaintsController> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ComplaintDto>>> List(
        [FromQuery] long? storeId = null,
        [FromQuery] long? technicianId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var pq = new PageQuery(page, pageSize, null);
        var q = _db.ServiceComplaints.AsNoTracking().AsQueryable();
        if (storeId.HasValue) q = q.Where(c => c.StoreId == storeId.Value);
        if (technicianId.HasValue) q = q.Where(c => c.OriginalTechnicianId == technicianId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ComplaintStatus>(status, true, out var st))
            q = q.Where(c => c.Status == st);
        if (from.HasValue) q = q.Where(c => c.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(c => c.CreatedAt < to.Value);

        var total = await q.CountAsync(ct);
        var rows = await q
            .Include(c => c.Order)
            .Include(c => c.OrderItem)
            .Include(c => c.OriginalTechnician)
            .Include(c => c.Member)
            .Include(c => c.ReassignedToTechnician)
            .Include(c => c.RecordedByUser)
            .Include(c => c.ResolvedByUser)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pq.SafePage - 1) * pq.SafePageSize)
            .Take(pq.SafePageSize)
            .ToListAsync(ct);

        return Ok(new PagedResult<ComplaintDto>(rows.Select(MapDto).ToList(), total, pq.SafePage, pq.SafePageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ComplaintDto>> Get(long id, CancellationToken ct)
    {
        var c = await _db.ServiceComplaints.AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.OrderItem)
            .Include(x => x.OriginalTechnician)
            .Include(x => x.Member)
            .Include(x => x.ReassignedToTechnician)
            .Include(x => x.RecordedByUser)
            .Include(x => x.ResolvedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? NotFound() : Ok(MapDto(c));
    }

    [HttpPost]
    public async Task<ActionResult<ComplaintDto>> Create(
        [FromBody] CreateComplaintRequest req, CancellationToken ct)
    {
        var item = await _db.OrderItems
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.Id == req.OrderItemId, ct);
        if (item is null) return NotFound(new { code = "OrderItemNotFound", message = "订单项不存在" });

        var existing = await _db.ServiceComplaints.AnyAsync(c =>
            c.OrderItemId == req.OrderItemId && c.Status == ComplaintStatus.Pending, ct);
        if (existing) return Conflict(new { code = "AlreadyPending", message = "该订单项已有未处理投诉" });

        var complaint = new ServiceComplaint
        {
            StoreId = item.Order.StoreId,
            OrderId = item.OrderId,
            OrderItemId = item.Id,
            OriginalTechnicianId = item.TechnicianId,
            MemberId = item.Order.MemberId,
            Tags = string.IsNullOrWhiteSpace(req.Tags) ? null : req.Tags.Trim(),
            Comment = string.IsNullOrWhiteSpace(req.Comment) ? null : req.Comment.Trim(),
            Status = ComplaintStatus.Pending,
            RecordedByUserId = _tenantContext.UserId
        };
        _db.ServiceComplaints.Add(complaint);

        // 投诉登记同时通知店长
        var techName = (await _db.Users.AsNoTracking()
            .Where(u => u.Id == item.TechnicianId)
            .Select(u => u.RealName ?? u.Username)
            .FirstOrDefaultAsync(ct)) ?? "技师";
        var nowTicks = DateTime.UtcNow.Ticks;
        _db.NotificationOutbox.Add(new NotificationOutbox
        {
            TenantId = _tenantContext.TenantId,
            Kind = NotificationKind.ServiceComplaintAlert,
            Status = NotificationStatus.Pending,
            DedupKey = $"Complaint:{item.Id}:{nowTicks}",
            MemberId = item.Order.MemberId,
            Title = "客户投诉登记",
            Body = $"订单 {item.Order.OrderNo} 投诉技师 {techName}"
                   + (string.IsNullOrWhiteSpace(req.Tags) ? "" : $"（{req.Tags}）"),
            RelatedEntityId = item.Id,
            ScheduledAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Complaint created complaint={ComplaintId} order={OrderId} item={ItemId} tech={Tech}",
            complaint.Id, item.OrderId, item.Id, item.TechnicianId);

        return Ok(await GetDtoAsync(complaint.Id, ct));
    }

    [HttpPatch("{id:long}/resolve")]
    public async Task<ActionResult<ComplaintDto>> Resolve(
        long id, [FromBody] ResolveComplaintRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<ComplaintResolution>(req.Resolution, true, out var resolution))
            return BadRequest(new { code = "InvalidResolution", message = "处理方式不合法" });

        var complaint = await _db.ServiceComplaints
            .Include(c => c.OrderItem).ThenInclude(i => i.Order)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (complaint is null) return NotFound();
        if (complaint.Status != ComplaintStatus.Pending)
            return Conflict(new { code = "AlreadyResolved", message = "该投诉已处理或已取消" });

        if (resolution == ComplaintResolution.Reassigned)
        {
            if (req.ReassignedToTechnicianId is not long newTechId)
                return BadRequest(new { code = "MissingTechnician", message = "改派需指定新技师" });
            if (newTechId == complaint.OriginalTechnicianId)
                return BadRequest(new { code = "SameTechnician", message = "新技师与原技师相同" });

            var item = complaint.OrderItem;
            if (item.Order.Status != OrderStatus.Pending && item.Order.Status != OrderStatus.InProgress)
                return Conflict(new { code = "InvalidOrderState", message = "订单已结账或已取消，无法改派" });

            var newTech = await _db.Users.FirstOrDefaultAsync(u =>
                u.Id == newTechId && u.Role == UserRole.Technician && u.IsActive, ct);
            if (newTech is null) return BadRequest(new { code = "TechnicianNotFound", message = "新技师不存在或已停用" });

            item.PreviousTechnicianId = item.TechnicianId;
            item.TechnicianId = newTechId;
            item.TransferredAt = DateTime.UtcNow;
            item.TransferReason = string.IsNullOrWhiteSpace(req.ResolutionNote)
                ? "投诉改派"
                : "投诉改派：" + req.ResolutionNote.Trim();
            item.ComplaintTransferred = true;

            complaint.ReassignedToTechnicianId = newTechId;
        }

        complaint.Status = ComplaintStatus.Resolved;
        complaint.Resolution = resolution;
        complaint.ResolutionNote = string.IsNullOrWhiteSpace(req.ResolutionNote) ? null : req.ResolutionNote.Trim();
        complaint.ResolvedAt = DateTime.UtcNow;
        complaint.ResolvedByUserId = _tenantContext.UserId;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Complaint resolved complaint={Id} resolution={Resolution} reassignTo={ReTech}",
            complaint.Id, resolution, complaint.ReassignedToTechnicianId);

        return Ok(await GetDtoAsync(id, ct));
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<ActionResult<ComplaintDto>> Cancel(long id, CancellationToken ct)
    {
        var complaint = await _db.ServiceComplaints.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (complaint is null) return NotFound();
        if (complaint.Status != ComplaintStatus.Pending)
            return Conflict(new { code = "InvalidState", message = "只有待处理投诉可取消" });
        complaint.Status = ComplaintStatus.Cancelled;
        complaint.ResolvedAt = DateTime.UtcNow;
        complaint.ResolvedByUserId = _tenantContext.UserId;
        await _db.SaveChangesAsync(ct);
        return Ok(await GetDtoAsync(id, ct));
    }

    private async Task<ComplaintDto> GetDtoAsync(long id, CancellationToken ct)
    {
        var c = await _db.ServiceComplaints.AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.OrderItem)
            .Include(x => x.OriginalTechnician)
            .Include(x => x.Member)
            .Include(x => x.ReassignedToTechnician)
            .Include(x => x.RecordedByUser)
            .Include(x => x.ResolvedByUser)
            .FirstAsync(x => x.Id == id, ct);
        return MapDto(c);
    }

    private static ComplaintDto MapDto(ServiceComplaint c) => new(
        c.Id, c.StoreId,
        c.OrderId, c.Order?.OrderNo ?? string.Empty,
        c.OrderItemId, c.OrderItem?.ServiceName ?? string.Empty,
        c.OriginalTechnicianId,
        c.OriginalTechnician?.RealName ?? c.OriginalTechnician?.Username ?? string.Empty,
        c.MemberId, c.Member?.Name ?? c.Member?.CardNo,
        c.Tags, c.Comment,
        c.Status.ToString(),
        c.Resolution?.ToString(),
        c.ReassignedToTechnicianId,
        c.ReassignedToTechnician?.RealName ?? c.ReassignedToTechnician?.Username,
        c.ResolutionNote,
        c.RecordedByUser?.RealName ?? c.RecordedByUser?.Username,
        c.ResolvedByUser?.RealName ?? c.ResolvedByUser?.Username,
        c.ResolvedAt, c.CreatedAt);
}
