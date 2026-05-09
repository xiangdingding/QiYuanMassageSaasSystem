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

public record SetQueueStateRequest(string State);

public record CallNextRequest(long StoreId);

public record CallNextResultDto(
    long? TechnicianId,
    string? TechnicianName,
    int? EmployeeNo,
    int Position);
