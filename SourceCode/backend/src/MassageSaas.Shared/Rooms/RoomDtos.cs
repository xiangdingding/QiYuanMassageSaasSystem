namespace MassageSaas.Shared.Rooms;

public record RoomDto(
    long Id,
    long StoreId,
    string RoomNo,
    int Capacity,
    string? RoomType,
    string? Remark,
    bool IsActive,
    bool IsOccupied,
    long? OccupiedByOrderId,
    string? OccupiedByOrderNo,
    bool IsTimedRoom,
    decimal HourlyRate);

public record CreateRoomRequest(
    long StoreId,
    string RoomNo,
    int Capacity,
    string? RoomType,
    string? Remark,
    bool IsTimedRoom = false,
    decimal HourlyRate = 0m);

public record UpdateRoomRequest(
    string RoomNo,
    int Capacity,
    string? RoomType,
    string? Remark,
    bool IsActive,
    bool IsTimedRoom = false,
    decimal HourlyRate = 0m);

// ---- 计时房 ----

public record TimedRoomSessionDto(
    long Id,
    long StoreId,
    long RoomId,
    string RoomNo,
    long? MemberId,
    string? MemberName,
    string? CustomerName,
    DateTime StartedAt,
    DateTime? EndedAt,
    decimal HourlyRateSnapshot,
    int BilledMinutes,
    int ElapsedMinutes,
    decimal Amount,
    string PayMethod,
    string Status,
    string? OperatorName,
    string? Remark);

public record StartTimedRoomRequest(
    long? MemberId,
    string? CustomerName,
    string? Remark);

public record StopTimedRoomRequest(string PayMethod);
