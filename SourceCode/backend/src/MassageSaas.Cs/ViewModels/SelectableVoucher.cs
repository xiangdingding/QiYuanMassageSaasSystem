using CommunityToolkit.Mvvm.ComponentModel;
using MassageSaas.Shared.Vouchers;

namespace MassageSaas.Cs.ViewModels;

/// <summary>
/// 给 DataGrid 多选用的轻包装：保留原 VoucherDto 同时承载 IsSelected。
/// 透传常用展示字段，让 XAML Binding 不必每处写 "Dto.Xxx"。
/// </summary>
public partial class SelectableVoucher : ObservableObject
{
    public VoucherDto Dto { get; }

    [ObservableProperty]
    private bool isSelected;

    public SelectableVoucher(VoucherDto dto)
    {
        Dto = dto;
    }

    public long Id => Dto.Id;
    public string Code => Dto.Code;
    public string Title => Dto.Title;
    public string Kind => Dto.Kind;
    public decimal FaceValue => Dto.FaceValue;
    public decimal MinOrderAmount => Dto.MinOrderAmount;
    public decimal? DiscountPercent => Dto.DiscountPercent;
    public DateTime? ExpiresAt => Dto.ExpiresAt;
    public string Status => Dto.Status;

    // ---- 展示用（对齐 BS：类型/状态中文化、折扣折数、平台占位、到期文案）----
    public string KindLabel => Dto.Kind == "GroupBuy" ? "团购券" : "店内券";
    public string PlatformText => string.IsNullOrWhiteSpace(Dto.Platform) ? "—" : Dto.Platform!;
    public string DiscountText => Dto.DiscountPercent is decimal p && p > 0 ? $"{p * 10m:0.#}折" : "—";
    public string ExpiryText => Dto.ExpiresAt is DateTime dt ? dt.ToString("yyyy-MM-dd HH:mm:ss") : "长期有效";
    public string StatusLabel => Dto.Status switch
    {
        "Active" => "生效中",
        "Redeemed" => "已核销",
        "Expired" => "已过期",
        "Cancelled" => "已作废",
        _ => Dto.Status
    };
    /// <summary>仅生效中的券可作废（对齐 BS 作废按钮 disabled 规则）。</summary>
    public bool CanCancel => Dto.Status == "Active";
}
