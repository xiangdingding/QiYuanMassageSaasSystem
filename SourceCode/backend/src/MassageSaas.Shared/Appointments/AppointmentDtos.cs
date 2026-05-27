namespace MassageSaas.Shared.Appointments;

public record CreateAppointmentRequest(
    long StoreId,
    long? ServiceId,
    long? PreferredTechnicianId,
    string CustomerName,
    string CustomerPhone,
    string? CustomerOpenId,
    DateTime ExpectedArriveAt,
    int PartySize,
    string? Remark);

public record AppointmentDto(
    long Id,
    long StoreId,
    string StoreName,
    long? ServiceId,
    string? ServiceName,
    long? PreferredTechnicianId,
    string? PreferredTechnicianName,
    string CustomerName,
    string CustomerPhone,
    DateTime ExpectedArriveAt,
    int PartySize,
    string Status,
    string? Remark,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    DateTime? ArrivedAt,
    DateTime? CancelledAt,
    string? CancelReason);

public record CancelAppointmentRequest(string? Reason);

public record ConfirmAppointmentRequest(string? Remark);

/// <summary>修改待确认的预约：StoreId 不可改（跨店改单要走"取消+重排"），其它字段同 Create。</summary>
public record UpdateAppointmentRequest(
    long? ServiceId,
    long? PreferredTechnicianId,
    string CustomerName,
    string CustomerPhone,
    DateTime ExpectedArriveAt,
    int PartySize,
    string? Remark);
