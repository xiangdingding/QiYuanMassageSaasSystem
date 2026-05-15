namespace MassageSaas.Shared.Queue;

public record TechnicianQueueItemDto(
    long Id,
    long TechnicianId,
    string TechnicianName,
    int? EmployeeNo,
    string State,
    int QueuePosition,
    int TodayRoundCount,
    DateTime? EnteredAt,
    DateTime? LastCalledAt);

/// <summary>
/// 给小程序技师端的"我的状态"DTO：在排队信息基础上多带当前服务房间和"上钟前必读"。
/// 客户为散客（无 MemberId）时所有 Customer* / *Notes 字段为 null。
/// </summary>
public record MyQueueDto(
    long? Id,
    long TechnicianId,
    string State,
    int QueuePosition,
    int TodayRoundCount,
    DateTime? EnteredAt,
    DateTime? LastCalledAt,
    string? CurrentRoomNo,
    long? CurrentOrderId,
    string? CurrentServiceName,
    string? CurrentCustomerName,
    string? CurrentCustomerGender,
    string? CurrentCustomerPreferences,
    string? CurrentCustomerHealth,
    bool CurrentCustomerHasNotes);

public record SetQueueStateRequest(string State);

public record CallNextRequest(long StoreId);

public record CallNextResultDto(
    long? TechnicianId,
    string? TechnicianName,
    int? EmployeeNo,
    int Position);
