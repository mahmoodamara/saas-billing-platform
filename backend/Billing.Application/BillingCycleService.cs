using Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Billing.Application;

public sealed class BillingCycleService(IBillingDbContext db, InvoiceService invoices, AuditService audit)
{
    public async Task<int> ProcessDueAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var dueIds = await db.Subscriptions
            .Where(x => x.Status == SubscriptionStatus.Active && x.BillingPeriodEnd <= now)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var processed = 0;
        foreach (var id in dueIds)
        {
            // A subscription may be several periods behind after downtime, so catch it up.
            while (true)
            {
                var sub = await db.Subscriptions.Include(x => x.Plan).Include(x => x.ScheduledPlan)
                    .FirstAsync(x => x.Id == id, ct);
                if (sub.BillingPeriodEnd > now || sub.Status != SubscriptionStatus.Active) break;

                await invoices.GenerateCurrentAsync(sub.OrganizationId, Guid.Empty, ct);
                var oldEnd = sub.BillingPeriodEnd;
                if (sub.ScheduledPlanId.HasValue)
                {
                    var oldPlan = sub.Plan.Name;
                    var newPlan = sub.ScheduledPlan!;
                    sub.PlanId = newPlan.Id;
                    sub.Plan = newPlan;
                    sub.ScheduledPlanId = null;
                    sub.ScheduledPlan = null;
                    await db.SaveChangesAsync(ct);
                    await audit.WriteAsync("ScheduledDowngradeApplied", nameof(Subscription), sub.Id.ToString(), sub.OrganizationId, null, new { from = oldPlan, to = newPlan.Name }, ct);
                }

                sub.BillingPeriodStart = oldEnd;
                sub.BillingPeriodEnd = oldEnd.AddMonths(1);
                await db.SaveChangesAsync(ct);
                await audit.WriteAsync("BillingCycleAdvanced", nameof(Subscription), sub.Id.ToString(), sub.OrganizationId, null, new { sub.BillingPeriodStart, sub.BillingPeriodEnd }, ct);
                processed++;
            }
        }
        return processed;
    }
}
