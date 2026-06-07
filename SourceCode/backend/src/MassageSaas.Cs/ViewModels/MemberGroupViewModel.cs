using System;
using System.Collections.Generic;
using System.Linq;
using MassageSaas.Shared.Members;

namespace MassageSaas.Cs.ViewModels;

/// <summary>按手机号聚合的会员组（一人多卡）。组级汇总 + 名下卡列表，UI 用 Expander 展开。</summary>
public class MemberGroupViewModel
{
    public string Phone { get; }
    public string? PrimaryName { get; }
    public int CardCount { get; }
    public decimal TotalBalance { get; }
    public decimal TotalRecharge { get; }
    public decimal TotalConsumed { get; }
    public bool HasInactive { get; }
    public List<MemberCardRowViewModel> Cards { get; }

    public string DisplayName => string.IsNullOrWhiteSpace(PrimaryName) ? "未填姓名" : PrimaryName!;

    public MemberGroupViewModel(MemberPhoneGroupDto g)
    {
        Phone = g.Phone;
        PrimaryName = g.PrimaryName;
        CardCount = g.CardCount;
        TotalBalance = g.TotalBalance;
        TotalRecharge = g.TotalRecharge;
        TotalConsumed = g.TotalConsumed;
        HasInactive = g.HasInactive;
        Cards = g.Cards.Select(c => new MemberCardRowViewModel(c)).ToList();
    }
}

/// <summary>会员卡行：包一张 <see cref="MemberDto"/>，给徽标/可用性/有效期等算好展示，供卡操作命令用 <see cref="Dto"/>。</summary>
public class MemberCardRowViewModel
{
    public MemberDto Dto { get; }

    public MemberCardRowViewModel(MemberDto dto) => Dto = dto;

    public string CardNo => Dto.CardNo;
    public bool IsActive => Dto.IsActive;
    public decimal Balance => Dto.Balance;
    public decimal TotalRecharge => Dto.TotalRecharge;
    public decimal TotalConsumed => Dto.TotalConsumed;

    public string? MemberTypeName => Dto.MemberTypeName;
    public bool HasMemberType => !string.IsNullOrEmpty(Dto.MemberTypeName);
    public bool IsCountBased => Dto.MemberTypeKind == "CountBased";
    public string CountText => $"{Dto.TotalCount ?? 0} / 剩 {Dto.RemainCount ?? 0} 次";

    public bool ShowDiscount => Dto.Discount < 1m;
    public string DiscountText => $"{Dto.Discount * 10m:0.#} 折";

    public bool ShowClosed => !Dto.IsActive;

    /// <summary>充值仅活动卡可用；退卡/转赠还需余额 &gt; 0（与 BS 一致）。</summary>
    public bool CanRecharge => Dto.IsActive;
    public bool CanRefundOrTransfer => Dto.IsActive && Dto.Balance > 0m;

    public string CardStartText => Dto.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

    public bool IsExpired => Dto.CardDaysRemaining is int d && d < 0;

    public string ValidityText
    {
        get
        {
            if (Dto.CardExpiresAt is not DateTime exp) return "永久有效";
            var date = exp.ToString("yyyy-MM-dd");
            var d = Dto.CardDaysRemaining;
            if (d is null) return $"到期 {date}";
            if (d < 0) return $"已过期（{date}）";
            if (d == 0) return $"今天到期（{date}）";
            return $"到期 {date}（剩 {d} 天）";
        }
    }
}
