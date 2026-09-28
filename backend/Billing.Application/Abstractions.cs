using Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Billing.Application;

public interface IBillingDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<User> Users { get; }
    DbSet<Plan> Plans { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<UsageEvent> UsageEvents { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentAttempt> PaymentAttempts { get; }
    DbSet<Refund> Refunds { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IUsageCounter
{
    Task<long?> GetAsync(Guid organizationId, DateTime periodStart);
    Task<long> IncrementAsync(Guid organizationId, DateTime periodStart, int quantity);
    Task SetAsync(Guid organizationId, DateTime periodStart, long quantity);
}

public sealed record ProviderChargeResult(bool Success, string ProviderReference, string? Error = null);

public interface IPaymentProvider
{
    Task<ProviderChargeResult> ChargeAsync(decimal amount, string currency, string idempotencyKey, bool simulateFailure, CancellationToken cancellationToken);
    Task<ProviderChargeResult> RefundAsync(string providerReference, decimal amount, CancellationToken cancellationToken);
}

public interface IAuditIndexer
{
    Task IndexAsync(AuditLog log, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);
}

public sealed record AuditSearchResult(string Action, string EntityType, string EntityId, string DataJson, DateTime CreatedAt);
