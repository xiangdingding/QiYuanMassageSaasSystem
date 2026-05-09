using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.Common;
using MassageSaas.Shared.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

[ApiController]
[Route("api/members")]
[Authorize(Policy = "ShopStaff")]
public class MembersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public MembersController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<MemberDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] long? storeId = null,
        CancellationToken ct = default)
    {
        var pq = new PageQuery(page, pageSize, keyword);
        var q = _db.Members.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(m => m.CardNo.Contains(k) || m.Phone.Contains(k) || (m.Name != null && m.Name.Contains(k)));
        }
        if (storeId.HasValue) q = q.Where(m => m.StoreId == storeId.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(m => m.CreatedAt)
            .Skip((pq.SafePage - 1) * pq.SafePageSize)
            .Take(pq.SafePageSize)
            .Select(m => new MemberDto(
                m.Id, m.StoreId, m.CardNo, m.Phone, m.Name, m.Gender, m.Birthday,
                m.Balance, m.TotalRecharge, m.TotalConsumed, m.Discount, m.Remark, m.CreatedAt))
            .ToListAsync(ct);

        return Ok(new PagedResult<MemberDto>(items, total, pq.SafePage, pq.SafePageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<MemberDto>> Get(long id, CancellationToken ct)
    {
        var m = await _db.Members.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return NotFound();
        return Ok(new MemberDto(m.Id, m.StoreId, m.CardNo, m.Phone, m.Name, m.Gender, m.Birthday,
            m.Balance, m.TotalRecharge, m.TotalConsumed, m.Discount, m.Remark, m.CreatedAt));
    }

    [HttpPost]
    public async Task<ActionResult<MemberDto>> Create([FromBody] CreateMemberRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.CardNo) || string.IsNullOrWhiteSpace(req.Phone))
            return BadRequest(new { code = "InvalidInput", message = "卡号与手机号必填" });
        if (req.Discount <= 0 || req.Discount > 1)
            return BadRequest(new { code = "InvalidDiscount", message = "折扣需在 (0, 1] 范围内" });

        var dup = await _db.Members.AnyAsync(x => x.CardNo == req.CardNo, ct);
        if (dup) return Conflict(new { code = "DuplicateCardNo", message = "卡号已存在" });

        var store = await _db.Stores.FirstOrDefaultAsync(x => x.Id == req.StoreId, ct);
        if (store is null) return BadRequest(new { code = "StoreNotFound", message = "门店不存在" });

        var m = new Member
        {
            StoreId = req.StoreId,
            CardNo = req.CardNo.Trim(),
            Phone = req.Phone.Trim(),
            Name = req.Name?.Trim(),
            Gender = req.Gender,
            Birthday = req.Birthday,
            Discount = req.Discount,
            Remark = req.Remark,
            Balance = req.InitialBalance,
            TotalRecharge = req.InitialBalance,
            TotalConsumed = 0
        };
        _db.Members.Add(m);

        if (req.InitialBalance > 0)
        {
            await _db.SaveChangesAsync(ct);
            _db.MemberRechargeRecords.Add(new MemberRechargeRecord
            {
                MemberId = m.Id,
                StoreId = req.StoreId,
                Amount = req.InitialBalance,
                BonusAmount = 0,
                BalanceAfter = req.InitialBalance,
                PayMethod = PayMethod.Cash,
                OperatorUserId = _tenantContext.UserId,
                Remark = "开卡初始充值"
            });
        }
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = m.Id },
            new MemberDto(m.Id, m.StoreId, m.CardNo, m.Phone, m.Name, m.Gender, m.Birthday,
                m.Balance, m.TotalRecharge, m.TotalConsumed, m.Discount, m.Remark, m.CreatedAt));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<MemberDto>> Update(long id, [FromBody] UpdateMemberRequest req, CancellationToken ct)
    {
        var m = await _db.Members.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return NotFound();
        if (req.Discount <= 0 || req.Discount > 1)
            return BadRequest(new { code = "InvalidDiscount", message = "折扣需在 (0, 1] 范围内" });

        m.Phone = req.Phone.Trim();
        m.Name = req.Name?.Trim();
        m.Gender = req.Gender;
        m.Birthday = req.Birthday;
        m.Discount = req.Discount;
        m.Remark = req.Remark;
        await _db.SaveChangesAsync(ct);

        return Ok(new MemberDto(m.Id, m.StoreId, m.CardNo, m.Phone, m.Name, m.Gender, m.Birthday,
            m.Balance, m.TotalRecharge, m.TotalConsumed, m.Discount, m.Remark, m.CreatedAt));
    }

    [HttpPost("recharge")]
    public async Task<ActionResult<RechargeRecordDto>> Recharge([FromBody] RechargeRequest req, CancellationToken ct)
    {
        if (req.Amount <= 0)
            return BadRequest(new { code = "InvalidAmount", message = "充值金额必须 > 0" });
        if (req.BonusAmount < 0)
            return BadRequest(new { code = "InvalidBonus", message = "赠送金额不能为负" });
        if (!Enum.TryParse<PayMethod>(req.PayMethod, true, out var method) || method == PayMethod.MemberCard)
            return BadRequest(new { code = "InvalidPayMethod", message = "支付方式不合法（不能用会员卡充值）" });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var m = await _db.Members.FirstOrDefaultAsync(x => x.Id == req.MemberId, ct);
        if (m is null) return NotFound(new { code = "MemberNotFound", message = "会员不存在" });

        m.Balance += req.Amount + req.BonusAmount;
        m.TotalRecharge += req.Amount;

        var record = new MemberRechargeRecord
        {
            MemberId = m.Id,
            StoreId = m.StoreId,
            Amount = req.Amount,
            BonusAmount = req.BonusAmount,
            BalanceAfter = m.Balance,
            PayMethod = method,
            OperatorUserId = _tenantContext.UserId,
            Remark = req.Remark
        };
        _db.MemberRechargeRecords.Add(record);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Ok(new RechargeRecordDto(
            record.Id, m.Id, record.Amount, record.BonusAmount, record.BalanceAfter,
            method.ToString(), null, record.Remark, record.CreatedAt));
    }

    [HttpGet("{id:long}/recharges")]
    public async Task<ActionResult<IReadOnlyList<RechargeRecordDto>>> RechargeHistory(long id, CancellationToken ct)
    {
        var data = await _db.MemberRechargeRecords.AsNoTracking()
            .Include(r => r.OperatorUser)
            .Where(r => r.MemberId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(200)
            .Select(r => new RechargeRecordDto(
                r.Id, r.MemberId, r.Amount, r.BonusAmount, r.BalanceAfter,
                r.PayMethod.ToString(),
                r.OperatorUser != null ? (r.OperatorUser.RealName ?? r.OperatorUser.Username) : null,
                r.Remark, r.CreatedAt))
            .ToListAsync(ct);
        return Ok(data);
    }

    [HttpGet("{id:long}/orders")]
    public async Task<ActionResult<IReadOnlyList<object>>> ConsumptionHistory(long id, CancellationToken ct)
    {
        var data = await _db.Orders.AsNoTracking()
            .Where(o => o.MemberId == id)
            .OrderByDescending(o => o.CreatedAt)
            .Take(200)
            .Select(o => new
            {
                o.Id,
                o.OrderNo,
                o.Total,
                o.PaidAmount,
                o.PayMethod,
                o.Status,
                o.CreatedAt,
                o.CompletedAt
            })
            .ToListAsync(ct);
        return Ok(data);
    }
}
