using Billing.Domain;

namespace Billing.Application;

public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string Token, string Email, string Role, Guid? OrganizationId);

public sealed record PlanRequest(string Name, decimal MonthlyPrice, int IncludedMessages, decimal ExtraMessagePrice, bool IsActive = true);
public sealed record PlanDto(Guid Id, string Name, decimal MonthlyPrice, int IncludedMessages, decimal ExtraMessagePrice, bool IsActive);

public sealed record SubscriptionRequest(Guid PlanId);
public sealed record ChangePlanRequest(Guid PlanId);
public sealed record SubscriptionDto(Guid Id, string Status, PlanDto Plan, PlanDto? ScheduledPlan, DateTime BillingPeriodStart, DateTime BillingPeriodEnd);

public sealed record UsageEventRequest(string ExternalEventId, string Channel, string EventType, int Quantity, DateTime Timestamp);
public sealed record UsagePostResponse(bool Duplicate, long CurrentUsage);
public sealed record ChannelUsageDto(string Channel, long Quantity);
public sealed record UsageSummaryDto(long Used, int Included, long Overage, decimal EstimatedOverageCost, IReadOnlyList<ChannelUsageDto> ByChannel, DateTime PeriodStart, DateTime PeriodEnd);

public sealed record InvoiceItemDto(Guid Id, string Type, string Description, decimal Quantity, decimal UnitPrice, decimal Amount);
public sealed record InvoiceDto(Guid Id, string Status, DateTime PeriodStart, DateTime PeriodEnd, decimal Total, DateTime DueAt, IReadOnlyList<InvoiceItemDto> Items);

public sealed record PayInvoiceRequest(bool SimulateFailure = false);
public sealed record PaymentDto(Guid Id, Guid InvoiceId, decimal Amount, string Currency, string Status, string ProviderReference, int Attempts, DateTime CreatedAt);
public sealed record RefundRequest(decimal Amount, string Reason);
public sealed record RetryPaymentResponse(Guid PaymentId, string Status, int Attempts);

public sealed record AdminPaymentDto(Guid Id, string Organization, Guid InvoiceId, decimal Amount, string Currency, string Status, int Attempts, decimal Refunded, DateTime CreatedAt);

public sealed record DashboardDto(string Organization, SubscriptionDto? Subscription, UsageSummaryDto? Usage, InvoiceDto? LatestInvoice, IReadOnlyList<PaymentDto> RecentPayments);
