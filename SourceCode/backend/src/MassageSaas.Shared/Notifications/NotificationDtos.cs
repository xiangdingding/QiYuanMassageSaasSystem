namespace MassageSaas.Shared.Notifications;

public record NotificationDto(
    long Id,
    string Kind,
    string Status,
    string DedupKey,
    long? MemberId,
    string? RecipientPhone,
    string? RecipientOpenId,
    string Title,
    string Body,
    long? RelatedEntityId,
    DateTime ScheduledAt,
    DateTime? SentAt,
    int RetryCount,
    string? ErrorMessage,
    DateTime CreatedAt);
