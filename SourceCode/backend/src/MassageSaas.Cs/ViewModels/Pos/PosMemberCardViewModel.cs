using CommunityToolkit.Mvvm.ComponentModel;
using MassageSaas.Shared.Members;

namespace MassageSaas.Cs.ViewModels.Pos;

/// <summary>
/// 收银台里的一张会员卡行：包一张 <see cref="MemberDto"/>，附"是否勾选用于结算 / 是否可勾选"。
/// 一人多卡：按手机号查出的所有卡各占一行，可勾选多张合并结算（首张勾选作主卡，其余为次要卡）。
/// 逻辑与 BS 端 PosView 的 memberCards / selectedCardIds 一致。
/// </summary>
public partial class PosMemberCardViewModel : ObservableObject
{
    public MemberDto Member { get; }

    public PosMemberCardViewModel(MemberDto member) => Member = member;

    /// <summary>勾选用于本次结算。第一张勾选的卡作主卡（order.MemberId），其余走 SecondaryMemberIds。</summary>
    [ObservableProperty]
    private bool isSelected;

    /// <summary>是否可结算：次卡必须其绑定服务在购物车里。由父 VM 随购物车变化刷新。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSelect))]
    [NotifyPropertyChangedFor(nameof(EligibilityHint))]
    [NotifyPropertyChangedFor(nameof(ShowEligibilityHint))]
    private bool isEligible = true;

    public long Id => Member.Id;
    public string CardNo => Member.CardNo;
    public decimal Balance => Member.Balance;
    public bool IsActive => Member.IsActive;
    public bool IsCountBased => Member.MemberTypeKind == "CountBased";
    public string TypeLabel => Member.MemberTypeName ?? "普通";
    public string? ServiceItemName => Member.ServiceItemName;
    public long? ServiceItemId => Member.ServiceItemId;

    public DateTime CreatedAt => Member.CreatedAt;
    public DateTime? CardExpiresAt => Member.CardExpiresAt;
    public int? CardDaysRemaining => Member.CardDaysRemaining;

    /// <summary>可勾选 = 活动卡 且（非次卡 或 次卡绑定服务在购物车）。已关闭卡只展示不可选。</summary>
    public bool CanSelect => Member.IsActive && IsEligible;

    /// <summary>剩余次数展示（仅次卡）。</summary>
    public string RemainText =>
        IsCountBased && Member.RemainCount is int n ? $"· 剩 {n} 次" : string.Empty;

    /// <summary>不可勾选时的原因提示（已关闭 / 次卡无匹配项目 / 充值卡余额为0）。</summary>
    public string EligibilityHint
    {
        get
        {
            if (!Member.IsActive) return "已关闭";
            if (IsCountBased && !IsEligible)
                return string.IsNullOrEmpty(Member.ServiceItemName) ? "无绑定服务" : "无匹配项目";
            if (!IsCountBased && Member.Balance <= 0m) return "余额为0";
            return string.Empty;
        }
    }

    public bool ShowEligibilityHint => !string.IsNullOrEmpty(EligibilityHint);
}
