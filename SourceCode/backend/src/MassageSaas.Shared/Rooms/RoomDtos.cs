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
    string? OccupiedByOrderNo);

public record CreateRoomRequest(
    long StoreId,
    string RoomNo,
    int Capacity,
    string? RoomType,
    string? Remark);

public record UpdateRoomRequest(
    string RoomNo,
    int Capacity,
    string? RoomType,
    string? Remark,
    bool IsActive);
