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

/// <summary>
/// 顾客小程序"待评价"列表项：一条已完成、尚未评价的订单项。
/// 顾客须先绑卡（openId↔会员），仅返回名下会员卡的服务记录。
/// </summary>
public record ReviewableItemDto(
    long OrderId,
    long OrderItemId,
    string OrderNo,
    long TechnicianId,
    string TechnicianName,
    string ServiceName,
    DateTime? CompletedAt);
