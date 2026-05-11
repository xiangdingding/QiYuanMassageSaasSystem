namespace MassageSaas.Shared.Reviews;

public record ServiceReviewDto(
    long Id,
    long OrderId,
    long OrderItemId,
    long TechnicianId,
    string TechnicianName,
    long? MemberId,
    string? MemberName,
    int Rating,
    string? Tags,
    string? Comment,
    DateTime CreatedAt);

public record SubmitReviewRequest(
    long OrderId,
    long OrderItemId,
    int Rating,
    string? Tags,
    string? Comment);

public record TechnicianReviewSummaryDto(
    long TechnicianId,
    string TechnicianName,
    int ReviewCount,
    decimal AverageRating);
