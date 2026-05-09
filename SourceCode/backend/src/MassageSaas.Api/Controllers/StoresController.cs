using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/stores")]
[Authorize]
public class StoresController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public StoresController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StoreDto>>> List(CancellationToken ct)
    {
        var data = await _db.Stores.AsNoTracking()
            .OrderBy(s => s.ParentStoreId == null ? 0 : 1)
            .ThenBy(s => s.Id)
            .Select(s => new StoreDto(
                s.Id, s.Name, s.Address, s.Phone, s.IsActive,
                s.ParentStoreId == null, s.ParentStoreId, s.CreatedAt))
            .ToListAsync(ct);
        return Ok(data);
    }

    [HttpPost]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<StoreDto>> Create([FromBody] CreateStoreRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { code = "InvalidInput", message = "门店名必填" });

        if (req.ParentStoreId.HasValue)
        {
            var parent = await _db.Stores.FirstOrDefaultAsync(s => s.Id == req.ParentStoreId.Value, ct);
            if (parent is null) return BadRequest(new { code = "ParentNotFound", message = "总店不存在" });
            if (parent.ParentStoreId is not null)
                return BadRequest(new { code = "InvalidParent", message = "上级必须是总店（一级门店）" });
        }
        else
        {
            var hasHq = await _db.Stores.AnyAsync(s => s.ParentStoreId == null, ct);
            if (hasHq) return Conflict(new { code = "HeadquartersExists", message = "总店已存在，新增分店时请指定上级" });
        }

        var store = new Store
        {
            Name = req.Name.Trim(),
            Address = req.Address?.Trim(),
            Phone = req.Phone?.Trim(),
            ParentStoreId = req.ParentStoreId,
            IsActive = true
        };
        _db.Stores.Add(store);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(List), null,
            new StoreDto(store.Id, store.Name, store.Address, store.Phone, store.IsActive,
                store.ParentStoreId == null, store.ParentStoreId, store.CreatedAt));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = "ShopStaff")]
    public async Task<ActionResult<StoreDto>> Update(long id, [FromBody] UpdateStoreRequest req, CancellationToken ct)
    {
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (store is null) return NotFound();

        store.Name = req.Name.Trim();
        store.Address = req.Address?.Trim();
        store.Phone = req.Phone?.Trim();
        store.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);

        return Ok(new StoreDto(store.Id, store.Name, store.Address, store.Phone, store.IsActive,
            store.ParentStoreId == null, store.ParentStoreId, store.CreatedAt));
    }
}
