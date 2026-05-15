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
    private readonly ILogger<MembersController> _logger;

    public MembersController(
        ApplicationDbContext db,
        ITenantContext tenantContext,
        ILogger<MembersController> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<MemberDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] long? storeId = null,
        [FromQuery] bool includeClosed = false,
        CancellationToken ct = default)
    {
        var pq = new PageQuery(page, pageSize, keyword);
        var q = _db.Members.AsNoTracking()
            .Include(m => m.ReferredByMember)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(m => m.CardNo.Contains(k) || m.Phone.Contains(k) || (m.Name != null && m.Name.Contains(k)));
        }
        if (storeId.HasValue) q = q.Where(m => m.StoreId == storeId.Value);
        if (!includeClosed) q = q.Where(m => m.IsActive);

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(m => m.CreatedAt)
            .Skip((pq.SafePage - 1) * pq.SafePageSize)
            .Take(pq.SafePageSize)
            .ToListAsync(ct);

        return Ok(new PagedResult<MemberDto>(rows.Select(MapDto).ToList(), total, pq.SafePage, pq.SafePageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<MemberDto>> Get(long id, CancellationToken ct)
    {
        var m = await _db.Members.AsNoTracking()
            .Include(x => x.ReferredByMember)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return NotFound();
        return Ok(MapDto(m));
    }

    /// <summary>按累计充值（开卡 + 后续充值）粗分等级。后期可按租户配置。</summary>
    internal static MemberLevel ComputeLevel(decimal totalRecharge) =>
        totalRecharge >= 10000m ? MemberLevel.Diamond :
        totalRecharge >= 5000m ? MemberLevel.Gold :
        totalRecharge >= 1000m ? MemberLevel.Silver :
        MemberLevel.Regular;

    private static MemberDto MapDto(Member m) => new(
        m.Id, m.StoreId, m.CardNo, m.Phone, m.Name, m.Gender, m.Birthday,
        m.Balance, m.TotalRecharge, m.TotalConsumed, m.Discount, m.Remark,
        m.Level.ToString(), m.PreferenceNotes, m.HealthNotes,
        m.IsActive, m.ClosedAt, m.CloseReason,
        m.ReferredByMemberId,
        m.ReferredByMember?.Name ?? m.ReferredByMember?.CardNo,
        m.ReferralRewardEarned,
        m.CreatedAt);

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

        if (req.ReferredByMemberId is long refId)
        {
            var refOk = await _db.Members.AnyAsync(x => x.Id == refId && x.IsActive, ct);
            if (!refOk) return BadRequest(new { code = "InvalidReferrer", message = "引荐人不存在或已停用" });
        }

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
            TotalConsumed = 0,
            Level = ComputeLevel(req.InitialBalance),
            ReferredByMemberId = req.ReferredByMemberId
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
                Kind = MemberRechargeKind.Recharge,
                OperatorUserId = _tenantContext.UserId,
                Remark = "开卡初始充值"
            });
        }
        await _db.SaveChangesAsync(ct);

        // 开卡的 InitialBalance 也算充值，按规则给引荐人返佣
        if (req.InitialBalance > 0 && m.ReferredByMemberId.HasValue)
        {
            await TryGrantReferralAsync(m, req.InitialBalance, ct);
        }

        await _db.Entry(m).Reference(x => x.ReferredByMember).LoadAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = m.Id }, MapDto(m));
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
        if (req.Level is { } lvl && Enum.TryParse<MemberLevel>(lvl, true, out var ml))
            m.Level = ml;
        m.PreferenceNotes = req.PreferenceNotes;
        m.HealthNotes = req.HealthNotes;

        if (req.ReferredByMemberId.HasValue && req.ReferredByMemberId.Value != m.ReferredByMemberId)
        {
            if (req.ReferredByMemberId.Value == m.Id)
                return BadRequest(new { code = "SelfReferral", message = "引荐人不能是本人" });
            var refOk = await _db.Members.AnyAsync(x => x.Id == req.ReferredByMemberId.Value && x.IsActive, ct);
            if (!refOk) return BadRequest(new { code = "InvalidReferrer", message = "引荐人不存在或已停用" });
            m.ReferredByMemberId = req.ReferredByMemberId;
        }
        await _db.SaveChangesAsync(ct);

        await _db.Entry(m).Reference(x => x.ReferredByMember).LoadAsync(ct);
        return Ok(MapDto(m));
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
        if (!m.IsActive) return Conflict(new { code = "MemberClosed", message = "会员卡已关闭，不能充值" });

        m.Balance += req.Amount + req.BonusAmount;
        m.TotalRecharge += req.Amount;
        m.Level = ComputeLevel(m.TotalRecharge);

        var record = new MemberRechargeRecord
        {
            MemberId = m.Id,
            StoreId = m.StoreId,
            Amount = req.Amount,
            BonusAmount = req.BonusAmount,
            BalanceAfter = m.Balance,
            PayMethod = method,
            Kind = MemberRechargeKind.Recharge,
            OperatorUserId = _tenantContext.UserId,
            Remark = req.Remark
        };
        _db.MemberRechargeRecords.Add(record);

        await _db.SaveChangesAsync(ct);

        if (m.ReferredByMemberId.HasValue)
        {
            await TryGrantReferralAsync(m, req.Amount, ct);
        }

        await tx.CommitAsync(ct);

        return Ok(new RechargeRecordDto(
            record.Id, m.Id, record.Amount, record.BonusAmount, record.BalanceAfter,
            method.ToString(), record.Kind.ToString(),
            null, null, null, record.Remark, record.CreatedAt));
    }

    [HttpPost("{id:long}/refund")]
    public async Task<ActionResult<RechargeRecordDto>> Refund(
        long id, [FromBody] RefundMemberRequest req, CancellationToken ct)
    {
        if (req.RefundAmount <= 0)
            return BadRequest(new { code = "InvalidAmount", message = "退款金额必须 > 0" });
        if (!Enum.TryParse<PayMethod>(req.RefundMethod, true, out var method) || method == PayMethod.MemberCard)
            return BadRequest(new { code = "InvalidPayMethod", message = "退款方式不合法（不能退到会员卡）" });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var m = await _db.Members.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return NotFound();
        if (!m.IsActive) return Conflict(new { code = "AlreadyClosed", message = "会员卡已关闭，不能再退" });
        if (req.RefundAmount > m.Balance)
            return BadRequest(new { code = "InsufficientBalance", message = $"退款金额超过卡内余额 ¥{m.Balance:F2}" });

        m.Balance -= req.RefundAmount;
        m.IsActive = false;
        m.ClosedAt = DateTime.UtcNow;
        m.CloseReason = string.IsNullOrWhiteSpace(req.Reason) ? "退卡" : req.Reason!.Trim();

        var record = new MemberRechargeRecord
        {
            MemberId = m.Id,
            StoreId = m.StoreId,
            Amount = req.RefundAmount,
            BonusAmount = 0,
            BalanceAfter = m.Balance,
            PayMethod = method,
            Kind = MemberRechargeKind.Refund,
            OperatorUserId = _tenantContext.UserId,
            Remark = m.CloseReason
        };
        _db.MemberRechargeRecords.Add(record);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation("Member refund member={MemberId} amount={Amount} balanceAfter={Balance}",
            m.Id, req.RefundAmount, m.Balance);

        return Ok(new RechargeRecordDto(
            record.Id, m.Id, record.Amount, 0, record.BalanceAfter,
            method.ToString(), record.Kind.ToString(),
            null, null, null, record.Remark, record.CreatedAt));
    }

    [HttpPost("{id:long}/transfer")]
    public async Task<ActionResult<MemberDto>> Transfer(
        long id, [FromBody] TransferMemberRequest req, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var src = await _db.Members.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (src is null) return NotFound();
        if (!src.IsActive) return Conflict(new { code = "SourceClosed", message = "原会员卡已关闭" });
        if (src.Balance <= 0) return BadRequest(new { code = "NothingToTransfer", message = "余额为 0，无可转赠金额" });

        Member target;
        if (req.ToMemberId is long toId)
        {
            if (toId == src.Id) return BadRequest(new { code = "SelfTransfer", message = "不能转给自己" });
            var t = await _db.Members.FirstOrDefaultAsync(x => x.Id == toId, ct);
            if (t is null) return BadRequest(new { code = "TargetNotFound", message = "目标会员不存在" });
            if (!t.IsActive) return BadRequest(new { code = "TargetClosed", message = "目标会员已关闭" });
            target = t;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(req.NewMemberCardNo) || string.IsNullOrWhiteSpace(req.NewMemberPhone))
                return BadRequest(new { code = "InvalidNewMember", message = "新建目标会员需填卡号和电话" });
            var dup = await _db.Members.AnyAsync(x => x.CardNo == req.NewMemberCardNo.Trim(), ct);
            if (dup) return Conflict(new { code = "DuplicateCardNo", message = "新会员卡号已存在" });
            target = new Member
            {
                StoreId = src.StoreId,
                CardNo = req.NewMemberCardNo.Trim(),
                Phone = req.NewMemberPhone.Trim(),
                Name = req.NewMemberName?.Trim(),
                Discount = src.Discount,
                Balance = 0,
                TotalRecharge = 0,
                TotalConsumed = 0,
                Level = MemberLevel.Regular
            };
            _db.Members.Add(target);
            await _db.SaveChangesAsync(ct);
        }

        var amount = src.Balance;
        var reason = string.IsNullOrWhiteSpace(req.Reason) ? "卡转赠" : req.Reason!.Trim();

        // 原会员清空余额并关闭
        src.Balance = 0;
        src.IsActive = false;
        src.ClosedAt = DateTime.UtcNow;
        src.CloseReason = $"{reason}（转赠至 {target.CardNo}）";

        // 目标会员加余额（不计入 TotalRecharge，避免被算成"充值"刷等级）
        target.Balance += amount;

        _db.MemberRechargeRecords.Add(new MemberRechargeRecord
        {
            MemberId = src.Id,
            StoreId = src.StoreId,
            Amount = amount, BonusAmount = 0,
            BalanceAfter = src.Balance,
            PayMethod = PayMethod.MemberCard,
            Kind = MemberRechargeKind.TransferOut,
            CounterpartyMemberId = target.Id,
            OperatorUserId = _tenantContext.UserId,
            Remark = reason
        });
        _db.MemberRechargeRecords.Add(new MemberRechargeRecord
        {
            MemberId = target.Id,
            StoreId = target.StoreId,
            Amount = amount, BonusAmount = 0,
            BalanceAfter = target.Balance,
            PayMethod = PayMethod.MemberCard,
            Kind = MemberRechargeKind.TransferIn,
            CounterpartyMemberId = src.Id,
            OperatorUserId = _tenantContext.UserId,
            Remark = reason
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation("Member transfer src={Src} target={Target} amount={Amount}",
            src.Id, target.Id, amount);

        await _db.Entry(target).Reference(x => x.ReferredByMember).LoadAsync(ct);
        return Ok(MapDto(target));
    }

    [HttpGet("{id:long}/referrals")]
    public async Task<ActionResult<ReferralSummaryDto>> Referrals(long id, CancellationToken ct)
    {
        var me = await _db.Members.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (me is null) return NotFound();

        var referred = await _db.Members.AsNoTracking()
            .Where(x => x.ReferredByMemberId == id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ReferredMemberDto(
                x.Id, x.CardNo, x.Name, x.Phone, x.TotalRecharge, x.CreatedAt))
            .ToListAsync(ct);

        return Ok(new ReferralSummaryDto(
            me.Id, me.Name ?? me.CardNo, me.ReferralRewardEarned, referred.Count, referred));
    }

    [HttpGet("{id:long}/recharges")]
    public async Task<ActionResult<IReadOnlyList<RechargeRecordDto>>> RechargeHistory(long id, CancellationToken ct)
    {
        var data = await _db.MemberRechargeRecords.AsNoTracking()
            .Include(r => r.OperatorUser)
            .Include(r => r.CounterpartyMember)
            .Where(r => r.MemberId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(200)
            .Select(r => new RechargeRecordDto(
                r.Id, r.MemberId, r.Amount, r.BonusAmount, r.BalanceAfter,
                r.PayMethod.ToString(), r.Kind.ToString(),
                r.CounterpartyMemberId,
                r.CounterpartyMember != null ? (r.CounterpartyMember.Name ?? r.CounterpartyMember.CardNo) : null,
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

    /// <summary>给引荐人按租户配置百分比返佣。失败不影响主流程，只记录日志。</summary>
    private async Task TryGrantReferralAsync(Member rechargedMember, decimal rechargeAmount, CancellationToken ct)
    {
        if (rechargeAmount <= 0 || !rechargedMember.ReferredByMemberId.HasValue) return;

        var tenantId = rechargedMember.TenantId;
        var tenant = tenantId.HasValue
            ? await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId.Value, ct)
            : null;
        var pct = tenant?.ReferralRewardPercent ?? 0m;
        if (pct <= 0m || pct > 100m) return;

        var bonus = Math.Round(rechargeAmount * pct / 100m, 2, MidpointRounding.AwayFromZero);
        if (bonus <= 0m) return;

        var referrer = await _db.Members.FirstOrDefaultAsync(x => x.Id == rechargedMember.ReferredByMemberId!.Value, ct);
        if (referrer is null || !referrer.IsActive)
        {
            _logger.LogInformation("Referrer {RefId} missing or closed; skip bonus", rechargedMember.ReferredByMemberId);
            return;
        }

        referrer.Balance += bonus;
        referrer.ReferralRewardEarned += bonus;

        _db.MemberRechargeRecords.Add(new MemberRechargeRecord
        {
            MemberId = referrer.Id,
            StoreId = referrer.StoreId,
            Amount = bonus, BonusAmount = 0,
            BalanceAfter = referrer.Balance,
            PayMethod = PayMethod.MemberCard,
            Kind = MemberRechargeKind.ReferralBonus,
            CounterpartyMemberId = rechargedMember.Id,
            OperatorUserId = _tenantContext.UserId,
            Remark = $"引荐返佣 {pct:F1}%（来自会员 {rechargedMember.CardNo} 充值 ¥{rechargeAmount:F2}）"
        });
        await _db.SaveChangesAsync(ct);
    }
}
