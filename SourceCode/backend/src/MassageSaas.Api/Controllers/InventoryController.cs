using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Policy = "ShopStaff")]
public class InventoryController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public InventoryController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet("items")]
    public async Task<ActionResult<IReadOnlyList<InventoryItemDto>>> Items(
        [FromQuery] long storeId,
        [FromQuery] bool onlyLowStock = false,
        CancellationToken ct = default)
    {
        var q = _db.InventoryItems.AsNoTracking()
            .Where(x => x.StoreId == storeId && x.IsActive);
        if (onlyLowStock) q = q.Where(x => x.Quantity <= x.MinQuantity);
        var rows = await q.OrderBy(x => x.Code).ToListAsync(ct);
        return Ok(rows.Select(MapItem).ToList());
    }

    [HttpPost("items")]
    public async Task<ActionResult<InventoryItemDto>> CreateItem(
        [FromBody] CreateInventoryItemRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { code = "InvalidInput", message = "编码与名称必填" });
        var dup = await _db.InventoryItems.AnyAsync(x => x.StoreId == req.StoreId && x.Code == req.Code, ct);
        if (dup) return Conflict(new { code = "Duplicate", message = "该门店物品编码已存在" });
        var entity = new InventoryItem
        {
            StoreId = req.StoreId,
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            Unit = req.Unit,
            Quantity = req.Quantity,
            MinQuantity = req.MinQuantity,
            UnitCost = req.UnitCost,
            Remark = req.Remark
        };
        _db.InventoryItems.Add(entity);
        if (req.Quantity > 0)
        {
            await _db.SaveChangesAsync(ct);
            _db.InventoryMovements.Add(new InventoryMovement
            {
                ItemId = entity.Id,
                Kind = InventoryMovementKind.PurchaseIn,
                Delta = req.Quantity,
                QuantityAfter = req.Quantity,
                OperatorUserId = _tenantContext.UserId,
                Remark = "建档入库"
            });
        }
        await _db.SaveChangesAsync(ct);
        return Ok(MapItem(entity));
    }

    [HttpPut("items/{id:long}")]
    public async Task<ActionResult<InventoryItemDto>> UpdateItem(
        long id, [FromBody] UpdateInventoryItemRequest req, CancellationToken ct)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        item.Name = req.Name.Trim();
        item.Unit = req.Unit;
        item.MinQuantity = req.MinQuantity;
        item.UnitCost = req.UnitCost;
        item.Remark = req.Remark;
        item.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(MapItem(item));
    }

    [HttpPost("movements")]
    public async Task<ActionResult<InventoryMovementDto>> Move(
        [FromBody] CreateMovementRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<InventoryMovementKind>(req.Kind, true, out var kind))
            return BadRequest(new { code = "InvalidKind", message = "出入库类型不合法" });
        if (req.Delta == 0) return BadRequest(new { code = "ZeroDelta", message = "数量不能为 0" });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var item = await _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == req.ItemId, ct);
        if (item is null) return NotFound(new { code = "ItemNotFound", message = "物品不存在" });

        // 出库类（Consume/Discard）必须传负数；入库类（PurchaseIn）必须传正数；Adjust 允许正负
        var delta = req.Delta;
        if (kind == InventoryMovementKind.PurchaseIn && delta < 0) delta = -delta;
        if ((kind == InventoryMovementKind.Consume || kind == InventoryMovementKind.Discard) && delta > 0) delta = -delta;

        var newQty = item.Quantity + delta;
        if (newQty < 0)
            return Conflict(new { code = "Insufficient", message = "库存不足以扣减" });

        item.Quantity = newQty;
        var mv = new InventoryMovement
        {
            ItemId = item.Id,
            Kind = kind,
            Delta = delta,
            QuantityAfter = newQty,
            OperatorUserId = _tenantContext.UserId,
            Remark = req.Remark
        };
        _db.InventoryMovements.Add(mv);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await _db.Entry(mv).Reference(m => m.OperatorUser!).LoadAsync(ct);
        return Ok(MapMovement(mv, item.Name));
    }

    [HttpGet("movements")]
    public async Task<ActionResult<IReadOnlyList<InventoryMovementDto>>> Movements(
        [FromQuery] long itemId,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var rows = await _db.InventoryMovements.AsNoTracking()
            .Include(m => m.Item)
            .Include(m => m.OperatorUser)
            .Where(m => m.ItemId == itemId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(ct);
        return Ok(rows.Select(m => MapMovement(m, m.Item.Name)).ToList());
    }

    private static InventoryItemDto MapItem(InventoryItem i) => new(
        i.Id, i.StoreId, i.Code, i.Name, i.Unit,
        i.Quantity, i.MinQuantity, i.UnitCost, i.Remark, i.IsActive,
        LowStock: i.Quantity <= i.MinQuantity);

    private static InventoryMovementDto MapMovement(InventoryMovement m, string itemName) => new(
        m.Id, m.ItemId, itemName, m.Kind.ToString(),
        m.Delta, m.QuantityAfter,
        m.OperatorUserId,
        m.OperatorUser != null ? (m.OperatorUser.RealName ?? m.OperatorUser.Username) : null,
        m.Remark, m.CreatedAt);
}
