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
    public string? Platform => Dto.Platform;
    public decimal FaceValue => Dto.FaceValue;
    public decimal MinOrderAmount => Dto.MinOrderAmount;
    public decimal? DiscountPercent => Dto.DiscountPercent;
    public DateTime? ExpiresAt => Dto.ExpiresAt;
    public string Status => Dto.Status;
}
