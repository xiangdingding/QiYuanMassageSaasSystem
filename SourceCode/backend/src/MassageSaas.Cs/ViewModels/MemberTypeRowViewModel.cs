using MassageSaas.Shared.Members;

namespace MassageSaas.Cs.ViewModels;

/// <summary>
/// 会员类型列表行：包一条 <see cref="MemberTypeDto"/>，按卡种（充值卡/计次卡）算出各列展示文案。
/// 逻辑与 BS 端 MemberTypesView 表格一致。
/// </summary>
public class MemberTypeRowViewModel
{
    public MemberTypeDto Dto { get; }

    public MemberTypeRowViewModel(MemberTypeDto dto) => Dto = dto;

    public int Sort => Dto.Sort;
    public string Code => Dto.Code;
    public string Name => Dto.Name;

    public bool IsCountBased => Dto.Kind == "CountBased";
    public string KindLabel => IsCountBased ? "计次卡" : "充值卡";

    /// <summary>绑定服务 / 最低门槛：计次卡显示「服务 · ≥N 次」，充值卡显示「最低 ¥X」。</summary>
    public string BindingOrThreshold => IsCountBased
        ? $"{Dto.ServiceItemName ?? "—"} · ≥ {(Dto.MinPurchaseCount?.ToString() ?? "—")} 次"
        : $"最低 ¥{(Dto.MinRechargeAmount?.ToString("F2") ?? "—")}";

    public string DiscountLabel => Dto.Discount < 1m ? $"{Dto.Discount * 10m:0.#} 折" : "原价";

    /// <summary>赠送：充值卡送金额、计次卡送次数，无则 —。</summary>
    public string BonusLabel =>
        !IsCountBased && (Dto.BonusAmount ?? 0m) > 0m ? $"送 ¥{Dto.BonusAmount:F2}"
        : IsCountBased && (Dto.BonusCount ?? 0) > 0 ? $"送 {Dto.BonusCount} 次"
        : "—";

    public string ValidDaysLabel => Dto.ValidDays is int d && d > 0 ? $"{d} 天" : "永久";

    public string StatusLabel => Dto.IsActive ? "启用" : "停用";
}
