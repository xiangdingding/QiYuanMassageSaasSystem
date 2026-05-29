namespace MassageSaas.Shared.Complaints;

public record ComplaintDto(
    long Id,
    long StoreId,
    long? OrderId,
    string? OrderNo,
    long? OrderItemId,
    string? ServiceName,
    long? OriginalTechnicianId,
    string? OriginalTechnicianName,
    long? MemberId,
    string? MemberName,
    string? Tags,
    string? Comment,
    string Status,
    string? Resolution,
    long? ReassignedToTechnicianId,
    string? ReassignedToTechnicianName,
    string? ResolutionNote,
    string? RecordedByName,
    string? ResolvedByName,
    DateTime? ResolvedAt,
    DateTime CreatedAt);

public record CreateComplaintRequest(
    /// <summary>挂在订单项上的投诉填这个；匿名投诉留空，必须同时提供 StoreId。</summary>
    long? OrderItemId,
    string? Tags,
    string? Comment,
    /// <summary>匿名投诉（OrderItemId 为空）时必填，标识投诉归属门店。</summary>
    long? StoreId = null,
    /// <summary>匿名投诉时可选，标记被投诉技师。</summary>
    long? TechnicianId = null);

public record ResolveComplaintRequest(
    string Resolution,                    // Reassigned / Refunded / Apologized / NoAction
    long? ReassignedToTechnicianId,       // Resolution=Reassigned 时必填
    string? ResolutionNote);
