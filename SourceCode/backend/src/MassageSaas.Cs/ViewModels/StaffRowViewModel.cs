using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.ViewModels;

/// <summary>员工列表行：包一层 DTO，并解析「所属门店」名称（对齐 BS 列表的所属门店列）。</summary>
public sealed class StaffRowViewModel
{
    public StaffDto Dto { get; }

    /// <summary>所属门店名称；找不到时回退为「—」或「#Id」。</summary>
    public string StoreName { get; }

    public StaffRowViewModel(StaffDto dto, string storeName)
    {
        Dto = dto;
        StoreName = storeName;
    }
}
