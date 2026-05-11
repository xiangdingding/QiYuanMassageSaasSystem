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
/// 给小程序技师端的"我的状态"DTO：在排队信息基础上多带当前服务房间。
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
    string? CurrentServiceName);

public record SetQueueStateRequest(string State);

public record CallNextRequest(long StoreId);

public record CallNextResultDto(
    long? TechnicianId,
    string? TechnicianName,
    int? EmployeeNo,
    int Position);
