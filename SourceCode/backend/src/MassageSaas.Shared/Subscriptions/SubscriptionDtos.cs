namespace MassageSaas.Shared.Subscriptions;

public record CreateSubscriptionPaymentRequest(
    long TenantId,
    long PlanId,
    int Years,
    string Channel);

public record SubscriptionPaymentDto(
    long PaymentOrderId,
    string OrderNo,
    decimal Amount,
    string Channel,
    string Status,
    string? PayUrl,
    DateTime CreatedAt);

public record OfflineActivateRequest(
    long TenantId,
    long PlanId,
    int Years,
    decimal AmountReceived,
    string? Remark);

public record SubscriptionDto(
    long Id,
    long TenantId,
    long PlanId,
    string PlanName,
    DateTime StartAt,
    DateTime EndAt,
    string Source,
    string? Remark,
    DateTime CreatedAt);

public record TenantSubscriptionStatusDto(
    long TenantId,
    string Status,
    DateTime? ExpireAt,
    int? DaysToExpire,
    long? CurrentPlanId,
    string? CurrentPlanName);
