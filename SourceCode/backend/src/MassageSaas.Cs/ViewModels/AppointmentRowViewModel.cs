using MassageSaas.Shared.Appointments;

namespace MassageSaas.Cs.ViewModels;

/// <summary>
/// 预约列表行：包一条 <see cref="AppointmentDto"/>，给状态中文化并按状态算出可执行的操作。
/// 操作可见性与 BS 端 AppointmentsView 一致：
/// 待确认 → 确认/修改/到店/取消；已确认 → 到店/取消；已取消 → 再次预约。
/// </summary>
public class AppointmentRowViewModel
{
    public AppointmentDto Dto { get; }

    public AppointmentRowViewModel(AppointmentDto dto) => Dto = dto;

    public System.DateTime ExpectedArriveAt => Dto.ExpectedArriveAt;
    public string CustomerName => Dto.CustomerName;
    public string CustomerPhone => Dto.CustomerPhone;
    public int PartySize => Dto.PartySize;
    public string? ServiceName => Dto.ServiceName;
    public string? PreferredTechnicianName => Dto.PreferredTechnicianName;
    public string? Remark => Dto.Remark;

    public string StatusLabel => Dto.Status switch
    {
        "Pending" => "待确认",
        "Confirmed" => "已确认",
        "Arrived" => "已到店",
        "Completed" => "已完成",
        "Cancelled" => "已取消",
        "NoShow" => "未到店",
        _ => Dto.Status
    };

    public bool CanConfirm => Dto.Status == "Pending";
    public bool CanEdit => Dto.Status == "Pending";
    public bool CanArrive => Dto.Status is "Pending" or "Confirmed";
    public bool CanCancel => Dto.Status is "Pending" or "Confirmed";
    public bool CanRebook => Dto.Status == "Cancelled";
}
