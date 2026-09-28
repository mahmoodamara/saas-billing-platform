using Billing.Application;
using Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options), IBillingDbContext
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<UsageEvent> UsageEvents => Set<UsageEvent>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<UsageEvent>().HasIndex(x => x.ExternalEventId).IsUnique();
        b.Entity<Payment>().HasIndex(x => x.IdempotencyKey).IsUnique();
        b.Entity<IdempotencyRecord>().HasIndex(x => new { x.Key, x.Operation }).IsUnique();
        b.Entity<Invoice>().HasIndex(x => new { x.OrganizationId, x.SubscriptionId, x.PeriodStart, x.PeriodEnd }).IsUnique();
        b.Entity<Subscription>().HasOne(x => x.ScheduledPlan).WithMany().HasForeignKey(x => x.ScheduledPlanId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Subscription>().HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.NoAction);

        b.Entity<Plan>().Property(x => x.MonthlyPrice).HasPrecision(18, 2);
        b.Entity<Plan>().Property(x => x.ExtraMessagePrice).HasPrecision(18, 4);
        b.Entity<Invoice>().Property(x => x.Subtotal).HasPrecision(18, 2);
        b.Entity<Invoice>().Property(x => x.Total).HasPrecision(18, 2);
        b.Entity<InvoiceItem>().Property(x => x.Quantity).HasPrecision(18, 4);
        b.Entity<InvoiceItem>().Property(x => x.UnitPrice).HasPrecision(18, 4);
        b.Entity<InvoiceItem>().Property(x => x.Amount).HasPrecision(18, 2);
        b.Entity<Payment>().Property(x => x.Amount).HasPrecision(18, 2);
        b.Entity<Refund>().Property(x => x.Amount).HasPrecision(18, 2);
    }
}
