namespace Billing.Domain;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public List<User> Users { get; set; } = [];
    public List<Subscription> Subscriptions { get; set; } = [];
    public List<UsageEvent> UsageEvents { get; set; } = [];
    public List<Invoice> Invoices { get; set; } = [];
}

public sealed class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
}

public sealed class Plan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public int IncludedMessages { get; set; }
    public decimal ExtraMessagePrice { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Subscription : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;
    public Guid? ScheduledPlanId { get; set; }
    public Plan? ScheduledPlan { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime BillingPeriodStart { get; set; }
    public DateTime BillingPeriodEnd { get; set; }
    public DateTime? CancelledAt { get; set; }
}

public sealed class UsageEvent : BaseEntity
{
    public string ExternalEventId { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string Channel { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime Timestamp { get; set; }
}

public sealed class Invoice : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid? SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public DateTime DueAt { get; set; }
    public List<InvoiceItem> Items { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
}

public sealed class InvoiceItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public InvoiceItemType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public sealed class Payment : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string ProviderReference { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<PaymentAttempt> Attempts { get; set; } = [];
    public List<Refund> Refunds { get; set; } = [];
}

public sealed class PaymentAttempt : BaseEntity
{
    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public int AttemptNumber { get; set; }
    public PaymentStatus Status { get; set; }
    public string? Error { get; set; }
}

public sealed class Refund : BaseEntity
{
    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RefundStatus Status { get; set; } = RefundStatus.Succeeded;
}

public sealed class IdempotencyRecord : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
}

public sealed class AuditLog : BaseEntity
{
    public Guid? OrganizationId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string DataJson { get; set; } = "{}";
}
