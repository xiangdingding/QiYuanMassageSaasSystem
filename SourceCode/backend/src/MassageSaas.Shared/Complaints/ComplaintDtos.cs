namespace MassageSaas.Shared.Complaints;

public record ComplaintDto(
    long Id,
    long StoreId,
    long OrderId,
    string OrderNo,
    long OrderItemId,
    string ServiceName,
    long OriginalTechnicianId,
    string OriginalTechnicianName,
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
    long OrderItemId,
    string? Tags,
    string? Comment);

public record ResolveComplaintRequest(
    string Resolution,                    // Reassigned / Refunded / Apologized / NoAction
    long? ReassignedToTechnicianId,       // Resolution=Reassigned 时必填
    string? ResolutionNote);
