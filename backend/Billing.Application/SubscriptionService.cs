using Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Billing.Application;

public sealed class SubscriptionService(IBillingDbContext db, AuditService audit)
{
    public async Task<SubscriptionDto?> GetCurrentAsync(Guid organizationId, CancellationToken ct = default)
    {
        var sub = await db.Subscriptions.Include(x => x.Plan).Include(x => x.ScheduledPlan)
            .Where(x => x.OrganizationId == organizationId && x.Status != SubscriptionStatus.Cancelled)
            .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        return sub?.ToDto();
    }

    public async Task<SubscriptionDto> CreateAsync(Guid organizationId, Guid planId, Guid userId, CancellationToken ct = default)
    {
        var existing = await db.Subscriptions.AnyAsync(x => x.OrganizationId == organizationId && x.Status == SubscriptionStatus.Active, ct);
        if (existing) throw new InvalidOperationException("Organization already has an active subscription.");
        var plan = await db.Plans.FirstOrDefaultAsync(x => x.Id == planId && x.IsActive, ct) ?? throw new KeyNotFoundException("Plan not found.");
        var start = DateTime.UtcNow;
        var sub = new Subscription { OrganizationId = organizationId, PlanId = plan.Id, Plan = plan, BillingPeriodStart = start, BillingPeriodEnd = start.AddMonths(1), Status = SubscriptionStatus.Active };
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("SubscriptionCreated", nameof(Subscription), sub.Id.ToString(), organizationId, userId, new { plan.Name }, ct);
        return sub.ToDto();
    }

    public async Task<(SubscriptionDto Subscription, InvoiceDto? ProrationInvoice)> UpgradeAsync(Guid organizationId, Guid subscriptionId, Guid newPlanId, Guid userId, CancellationToken ct = default)
    {
        var sub = await db.Subscriptions.Include(x => x.Plan).Include(x => x.ScheduledPlan)
            .FirstOrDefaultAsync(x => x.Id == subscriptionId && x.OrganizationId == organizationId, ct) ?? throw new KeyNotFoundException("Subscription not found.");
        var newPlan = await db.Plans.FirstOrDefaultAsync(x => x.Id == newPlanId && x.IsActive, ct) ?? throw new KeyNotFoundException("Plan not found.");
        if (newPlan.Id == sub.PlanId) throw new InvalidOperationException("Already subscribed to this plan.");
        if (newPlan.MonthlyPrice <= sub.Plan.MonthlyPrice) throw new InvalidOperationException("Use downgrade for a lower-priced plan.");

        var adjustment = BillingCalculator.CalculateProration(sub.Plan.MonthlyPrice, newPlan.MonthlyPrice, sub.BillingPeriodStart, sub.BillingPeriodEnd, DateTime.UtcNow);
        var oldPlan = sub.Plan;
        sub.PlanId = newPlan.Id;
        sub.Plan = newPlan;
        sub.ScheduledPlanId = null;
        sub.ScheduledPlan = null;

        Invoice? invoice = null;
        if (adjustment > 0)
        {
            invoice = new Invoice
            {
                OrganizationId = organizationId,
                SubscriptionId = sub.Id,
                Status = InvoiceStatus.Open,
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = sub.BillingPeriodEnd,
                Subtotal = adjustment,
                Total = adjustment,
                DueAt = DateTime.UtcNow.AddDays(7),
                Items = [new InvoiceItem { Type = InvoiceItemType.Proration, Description = $"Upgrade {oldPlan.Name} → {newPlan.Name} (proration)", Quantity = 1, UnitPrice = adjustment, Amount = adjustment }]
            };
            db.Invoices.Add(invoice);
        }
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("SubscriptionUpgraded", nameof(Subscription), sub.Id.ToString(), organizationId, userId, new { from = oldPlan.Name, to = newPlan.Name, adjustment }, ct);
        return (sub.ToDto(), invoice?.ToDto());
    }

    public async Task<SubscriptionDto> DowngradeAsync(Guid organizationId, Guid subscriptionId, Guid newPlanId, Guid userId, CancellationToken ct = default)
    {
        var sub = await db.Subscriptions.Include(x => x.Plan).Include(x => x.ScheduledPlan)
            .FirstOrDefaultAsync(x => x.Id == subscriptionId && x.OrganizationId == organizationId, ct) ?? throw new KeyNotFoundException("Subscription not found.");
        var newPlan = await db.Plans.FirstOrDefaultAsync(x => x.Id == newPlanId && x.IsActive, ct) ?? throw new KeyNotFoundException("Plan not found.");
        if (newPlan.MonthlyPrice >= sub.Plan.MonthlyPrice) throw new InvalidOperationException("Use upgrade for an equal or higher-priced plan.");
        sub.ScheduledPlanId = newPlan.Id;
        sub.ScheduledPlan = newPlan;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("SubscriptionDowngradeScheduled", nameof(Subscription), sub.Id.ToString(), organizationId, userId, new { from = sub.Plan.Name, to = newPlan.Name, effective = sub.BillingPeriodEnd }, ct);
        return sub.ToDto();
    }

    public async Task<SubscriptionDto> CancelAsync(Guid organizationId, Guid subscriptionId, Guid userId, CancellationToken ct = default)
    {
        var sub = await db.Subscriptions.Include(x => x.Plan).Include(x => x.ScheduledPlan)
            .FirstOrDefaultAsync(x => x.Id == subscriptionId && x.OrganizationId == organizationId, ct) ?? throw new KeyNotFoundException("Subscription not found.");
        sub.Status = SubscriptionStatus.Cancelled;
        sub.CancelledAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("SubscriptionCancelled", nameof(Subscription), sub.Id.ToString(), organizationId, userId, null, ct);
        return sub.ToDto();
    }
}
