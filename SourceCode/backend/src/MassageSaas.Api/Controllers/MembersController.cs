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
            .Include(m => m.MemberType)
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

        var counts = await LoadCountCardSumsAsync(rows.Select(r => r.Id), ct);
        return Ok(new PagedResult<MemberDto>(rows.Select(m => MapDto(m, counts)).ToList(), total, pq.SafePage, pq.SafePageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<MemberDto>> Get(long id, CancellationToken ct)
    {
        var m = await _db.Members.AsNoTracking()
            .Include(x => x.ReferredByMember)
            .Include(x => x.MemberType)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return NotFound();
        var counts = await LoadCountCardSumsAsync(new[] { m.Id }, ct);
        return Ok(MapDto(m, counts));
    }

    /// <summary>按手机号分组：返回每人名下所有卡的聚合视图，方便"一人多卡"展开浏览。</summary>
    [HttpGet("grouped")]
    public async Task<ActionResult<PagedResult<MemberPhoneGroupDto>>> Grouped(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] long? storeId = null,
        [FromQuery] bool includeClosed = false,
        CancellationToken ct = default)
    {
        var pq = new PageQuery(page, pageSize, keyword);
        var baseQ = _db.Members.AsNoTracking().AsQueryable();
        if (storeId.HasValue) baseQ = baseQ.Where(m => m.StoreId == storeId.Value);
        if (!includeClosed) baseQ = baseQ.Where(m => m.IsActive);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            baseQ = baseQ.Where(m => m.CardNo.Contains(k) || m.Phone.Contains(k) || (m.Name != null && m.Name.Contains(k)));
        }

        // 1) 取分页内的手机号集合（先按最近活动倒序）
        var distinctPhones = baseQ
            .GroupBy(m => m.Phone)
            .Select(g => new { Phone = g.Key, LatestAt = g.Max(x => x.CreatedAt) });

        var total = await distinctPhones.CountAsync(ct);
        var pagedPhones = await distinctPhones
            .OrderByDescending(x => x.LatestAt)
            .Skip((pq.SafePage - 1) * pq.SafePageSize)
            .Take(pq.SafePageSize)
            .Select(x => x.Phone)
            .ToListAsync(ct);

        // 2) 拉这些手机号下的所有卡（受 includeClosed 控制，但忽略 keyword——
        //    keyword 仅决定"哪些手机号出现"，展开后看到这个人的全部卡）
        var cardsQ = _db.Members.AsNoTracking()
            .Include(m => m.ReferredByMember)
            .Include(m => m.MemberType)
            .Where(m => pagedPhones.Contains(m.Phone));
        if (storeId.HasValue) cardsQ = cardsQ.Where(m => m.StoreId == storeId.Value);
        if (!includeClosed) cardsQ = cardsQ.Where(m => m.IsActive);

        var allCards = await cardsQ.OrderByDescending(m => m.CreatedAt).ToListAsync(ct);
        var counts = await LoadCountCardSumsAsync(allCards.Select(c => c.Id), ct);

        // 3) 按手机号汇总
        var groups = pagedPhones.Select(phone =>
        {
            var cards = allCards.Where(m => m.Phone == phone).ToList();
            var firstNamed = cards.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Name));
            return new MemberPhoneGroupDto(
                phone,
                firstNamed?.Name ?? cards.FirstOrDefault()?.Name,
                cards.Count,
                cards.Where(c => c.IsActive).Sum(c => c.Balance),
                cards.Where(c => c.IsActive).Sum(c => c.TotalRecharge),
                cards.Where(c => c.IsActive).Sum(c => c.TotalConsumed),
                cards.Any(c => !c.IsActive),
                cards.Select(c => MapDto(c, counts)).ToList()
            );
        }).ToList();

        return Ok(new PagedResult<MemberPhoneGroupDto>(groups, total, pq.SafePage, pq.SafePageSize));
    }

    private static MemberDto MapDto(Member m, IReadOnlyDictionary<long, (int Total, int Remain)>? counts = null)
    {
        int? totalCount = null;
        int? remainCount = null;
        if (m.MemberType?.Kind == MemberTypeKind.CountBased && counts != null && counts.TryGetValue(m.Id, out var c))
        {
            totalCount = c.Total;
            remainCount = c.Remain;
        }

        return new MemberDto(
            m.Id, m.StoreId, m.CardNo, m.Phone, m.Name, m.Gender, m.Birthday,
            m.Balance, m.TotalRecharge, m.TotalConsumed, m.Discount, m.Remark,
            m.Level.ToString(), m.PreferenceNotes, m.HealthNotes,
            m.IsActive, m.ClosedAt, m.CloseReason,
            m.ReferredByMemberId,
            m.ReferredByMember?.Name ?? m.ReferredByMember?.CardNo,
            m.ReferralRewardEarned,
            m.WechatOpenId,
            m.CreatedAt,
            m.MemberTypeId,
            m.MemberType?.Name,
            m.MemberType?.Kind.ToString(),
            totalCount,
            remainCount);
    }

    /// <summary>
    /// 聚合一批会员的活动计次套餐：返回 memberId → (累计购买次数, 剩余次数)。
    /// 仅统计 Kind == Counter 且 Status == Active 的套餐。
    /// </summary>
    private async Task<Dictionary<long, (int Total, int Remain)>> LoadCountCardSumsAsync(
        IEnumerable<long> memberIds, CancellationToken ct)
    {
        var ids = memberIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<long, (int, int)>();

        var rows = await _db.MemberPackages.AsNoTracking()
            .Where(p => ids.Contains(p.MemberId)
                        && p.Kind == MemberPackageKind.Counter
                        && p.Status == MemberPackageStatus.Active)
            .GroupBy(p => p.MemberId)
            .Select(g => new
            {
                MemberId = g.Key,
                Total = g.Sum(x => x.TotalCount),
                Remain = g.Sum(x => x.RemainCount)
            })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.MemberId, x => (x.Total, x.Remain));
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

        if (req.ReferredByMemberId is long refId)
        {
            var refOk = await _db.Members.AnyAsync(x => x.Id == refId && x.IsActive, ct);
            if (!refOk) return BadRequest(new { code = "InvalidReferrer", message = "引荐人不存在或已停用" });
        }

        // 解析可选的会员类型模板
        MemberType? template = null;
        if (req.MemberTypeId is long typeId)
        {
            template = await _db.MemberTypes.FirstOrDefaultAsync(t => t.Id == typeId, ct);
            if (template is null)
                return BadRequest(new { code = "TypeNotFound", message = "会员类型不存在" });
            if (!template.IsActive)
                return BadRequest(new { code = "TypeInactive", message = "该会员类型已停用，请改选其它类型" });
        }

        // 按 Kind 校验最低门槛 & 计算赠送
        decimal bonusAmount = 0m;
        int bonusCount = 0;
        decimal effectiveDiscount = req.Discount;

        if (template is not null)
        {
            // 折扣以模板为准（覆盖前端传入）
            effectiveDiscount = template.Discount;

            if (template.Kind == MemberTypeKind.StoredValue)
            {
                if (template.MinRechargeAmount is decimal min && req.InitialBalance < min)
                    return BadRequest(new { code = "BelowMinAmount",
                        message = $"{template.Name} 最低充值 ¥{min:F2}，本次仅 ¥{req.InitialBalance:F2}" });
                bonusAmount = template.BonusAmount ?? 0m;
            }
            else // CountBased
            {
                if (template.MinPurchaseCount is int minCnt && req.Count < minCnt)
                    return BadRequest(new { code = "BelowMinCount",
                        message = $"{template.Name} 最低购买 {minCnt} 次，本次仅 {req.Count} 次" });
                bonusCount = template.BonusCount ?? 0;
            }
        }

        var now = DateTime.UtcNow;
        DateTime? cardExpiresAt = template?.ValidDays is int d && d > 0 ? now.AddDays(d) : null;

        // 充值卡：req.InitialBalance 是「充值金额」(面值)；次卡：req.InitialBalance 是「实收金额」(现金价 × 折扣)
        // 两种情况都把现金额写进 TotalRecharge / Balance，列表才能展示金额信息
        var initialPaid = req.InitialBalance;
        var initialBalance = initialPaid + bonusAmount;

        var m = new Member
        {
            StoreId = req.StoreId,
            CardNo = req.CardNo.Trim(),
            Phone = req.Phone.Trim(),
            Name = req.Name?.Trim(),
            Gender = req.Gender,
            Birthday = req.Birthday,
            Discount = effectiveDiscount,
            Remark = req.Remark,
            Balance = initialBalance,
            TotalRecharge = initialPaid,
            TotalConsumed = 0,
            Level = MemberLevel.Regular,
            MemberTypeId = template?.Id,
            ReferredByMemberId = req.ReferredByMemberId
        };
        _db.Members.Add(m);
        await _db.SaveChangesAsync(ct);

        // 解析支付来源：未传或不合法均回退 Cash；禁止用 MemberCard 给自己开卡充值
        var payMethod = PayMethod.Cash;
        if (!string.IsNullOrWhiteSpace(req.PayMethod)
            && Enum.TryParse<PayMethod>(req.PayMethod, true, out var parsed)
            && parsed != PayMethod.MemberCard)
        {
            payMethod = parsed;
        }

        // 充值卡或无模板的旧路径：写一条充值流水
        if (initialPaid > 0)
        {
            _db.MemberRechargeRecords.Add(new MemberRechargeRecord
            {
                MemberId = m.Id,
                StoreId = req.StoreId,
                Amount = initialPaid,
                BonusAmount = bonusAmount,
                BalanceAfter = initialBalance,
                PayMethod = payMethod,
                Kind = MemberRechargeKind.Recharge,
                OperatorUserId = _tenantContext.UserId,
                Remark = template != null ? $"开卡：{template.Name}" : "开卡初始充值"
            });
        }

        // 计次卡：建一张 MemberPackage 实例
        if (template is { Kind: MemberTypeKind.CountBased } && req.Count > 0)
        {
            var total = req.Count + bonusCount;
            _db.MemberPackages.Add(new MemberPackage
            {
                MemberId = m.Id,
                StoreId = req.StoreId,
                Kind = MemberPackageKind.Counter,
                ServiceId = template.ServiceItemId,
                Title = template.Name,
                PaidAmount = req.InitialBalance, // 实收金额（前端填的现金价）
                TotalCount = total,
                RemainCount = total,
                ValidFrom = now,
                ExpiresAt = cardExpiresAt,
                Status = MemberPackageStatus.Active,
                Remark = $"开卡：{template.Name}"
            });
        }

        await _db.SaveChangesAsync(ct);

        if (initialPaid > 0 && m.ReferredByMemberId.HasValue)
        {
            await TryGrantReferralAsync(m, initialPaid, ct);
        }

        await _db.Entry(m).Reference(x => x.ReferredByMember).LoadAsync(ct);
        await _db.Entry(m).Reference(x => x.MemberType).LoadAsync(ct);
        var createdCounts = await LoadCountCardSumsAsync(new[] { m.Id }, ct);
        return CreatedAtAction(nameof(Get), new { id = m.Id }, MapDto(m, createdCounts));
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
        if (req.WechatOpenId is not null)
            m.WechatOpenId = string.IsNullOrWhiteSpace(req.WechatOpenId) ? null : req.WechatOpenId.Trim();

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
        await _db.Entry(m).Reference(x => x.MemberType).LoadAsync(ct);
        var updatedCounts = await LoadCountCardSumsAsync(new[] { m.Id }, ct);
        return Ok(MapDto(m, updatedCounts));
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

        EnqueueRechargeArrivedNotification(m, req.Amount, req.BonusAmount);

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

    /// <summary>
    /// 给会员开一张某种类型的卡。
    /// - 充值卡：累加 Member.Balance（+赠送金额），写 MemberRechargeRecord；不创建 MemberPackage。
    /// - 计次卡：创建 MemberPackage（Kind=Counter，绑定模板的 ServiceItemId，TotalCount=次数+赠送次数）。
    /// 折扣会被复制到 Member.Discount（覆盖），ValidDays 决定 MemberPackage.ExpiresAt。
    /// </summary>
    [HttpPost("{id:long}/issue-card")]
    public async Task<ActionResult<IssueCardResultDto>> IssueCard(
        long id, [FromBody] IssueCardRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<PayMethod>(req.PayMethod, true, out var method))
            return BadRequest(new { code = "InvalidPayMethod", message = "支付方式不合法" });

        var member = await _db.Members.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (member is null) return NotFound(new { code = "MemberNotFound", message = "会员不存在" });
        if (!member.IsActive) return Conflict(new { code = "MemberClosed", message = "会员卡已关闭，不能办卡" });

        var template = await _db.MemberTypes
            .Include(t => t.ServiceItem)
            .FirstOrDefaultAsync(t => t.Id == req.MemberTypeId, ct);
        if (template is null)
            return BadRequest(new { code = "TypeNotFound", message = "会员类型不存在" });
        if (!template.IsActive)
            return BadRequest(new { code = "TypeInactive", message = "该会员类型已停用，无法开卡" });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var now = DateTime.UtcNow;
        DateTime? expiresAt = template.ValidDays is int days && days > 0 ? now.AddDays(days) : null;

        if (template.Kind == MemberTypeKind.StoredValue)
        {
            // ---- 充值卡：加钱进余额 ----
            if (method == PayMethod.MemberCard)
                return BadRequest(new { code = "InvalidPayMethod", message = "不能用会员卡余额给自己充值" });
            if (req.Amount <= 0)
                return BadRequest(new { code = "InvalidAmount", message = "充值金额必须 > 0" });
            if (template.MinRechargeAmount is decimal min && req.Amount < min)
                return BadRequest(new { code = "BelowMinAmount", message = $"{template.Name} 最低充值 ¥{min:F2}" });

            var bonus = template.BonusAmount ?? 0m;
            member.Balance += req.Amount + bonus;
            member.TotalRecharge += req.Amount;
            member.Discount = template.Discount;

            _db.MemberRechargeRecords.Add(new MemberRechargeRecord
            {
                MemberId = member.Id,
                StoreId = member.StoreId,
                Amount = req.Amount,
                BonusAmount = bonus,
                BalanceAfter = member.Balance,
                PayMethod = method,
                Kind = MemberRechargeKind.Recharge,
                OperatorUserId = _tenantContext.UserId,
                Remark = string.IsNullOrWhiteSpace(req.Remark) ? $"办卡：{template.Name}" : req.Remark
            });

            EnqueueRechargeArrivedNotification(member, req.Amount, bonus);

            await _db.SaveChangesAsync(ct);
            if (member.ReferredByMemberId.HasValue)
                await TryGrantReferralAsync(member, req.Amount, ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation("Issued StoredValue card type={TypeId} member={MemberId} amount={Amount} bonus={Bonus}",
                template.Id, member.Id, req.Amount, bonus);

            return Ok(new IssueCardResultDto(
                member.Id, template.Kind.ToString(), member.Balance,
                req.Amount, bonus, 0, null, expiresAt));
        }
        else
        {
            // ---- 计次卡：创建 MemberPackage ----
            if (template.ServiceItemId is null)
                return BadRequest(new { code = "ServiceMissing", message = "计次卡模板未绑定服务项目" });
            if (req.Count <= 0)
                return BadRequest(new { code = "InvalidCount", message = "购买次数必须 > 0" });
            if (template.MinPurchaseCount is int minCnt && req.Count < minCnt)
                return BadRequest(new { code = "BelowMinCount", message = $"{template.Name} 最低购买 {minCnt} 次" });

            var bonusCount = template.BonusCount ?? 0;
            var total = req.Count + bonusCount;

            var pkg = new MemberPackage
            {
                MemberId = member.Id,
                StoreId = member.StoreId,
                Kind = MemberPackageKind.Counter,
                ServiceId = template.ServiceItemId.Value,
                Title = template.Name,
                PaidAmount = req.Amount, // 收银侧填的现金价；不强制等于服务标准价
                TotalCount = total,
                RemainCount = total,
                ValidFrom = now,
                ExpiresAt = expiresAt,
                Status = MemberPackageStatus.Active,
                Remark = req.Remark
            };
            _db.MemberPackages.Add(pkg);
            member.Discount = template.Discount;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation("Issued CountBased card type={TypeId} member={MemberId} count={Count} bonus={Bonus} pkg={PkgId}",
                template.Id, member.Id, req.Count, bonusCount, pkg.Id);

            return Ok(new IssueCardResultDto(
                member.Id, template.Kind.ToString(), member.Balance,
                req.Amount, 0m, bonusCount, pkg.Id, expiresAt));
        }
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
        await _db.Entry(target).Reference(x => x.MemberType).LoadAsync(ct);
        var transferCounts = await LoadCountCardSumsAsync(new[] { target.Id }, ct);
        return Ok(MapDto(target, transferCounts));
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

    /// <summary>充值成功后写一条 RechargeArrived 通知到出箱。幂等键含 record 时间戳与会员，自然不重。</summary>
    private void EnqueueRechargeArrivedNotification(Member m, decimal amount, decimal bonus)
    {
        var nowTicks = DateTime.UtcNow.Ticks;
        var key = $"Recharge:{m.Id}:{nowTicks}";
        _db.NotificationOutbox.Add(new NotificationOutbox
        {
            TenantId = m.TenantId,
            Kind = NotificationKind.RechargeArrived,
            Status = NotificationStatus.Pending,
            DedupKey = key,
            MemberId = m.Id,
            RecipientPhone = m.Phone,
            RecipientOpenId = m.WechatOpenId,
            Title = "充值到账",
            Body = $"充值 ¥{amount:F2}{(bonus > 0 ? $" + 赠送 ¥{bonus:F2}" : "")} 已到账，余额 ¥{m.Balance:F2}",
            ScheduledAt = DateTime.UtcNow
        });
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
