using Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Billing.Application;

public sealed class DashboardService(IBillingDbContext db, SubscriptionService subscriptions, UsageService usage)
{
    public async Task<DashboardDto> GetAsync(Guid organizationId, CancellationToken ct = default)
    {
        var org = await db.Organizations.FirstOrDefaultAsync(x => x.Id == organizationId, ct) ?? throw new KeyNotFoundException("Organization not found.");
        var subscription = await subscriptions.GetCurrentAsync(organizationId, ct);
        UsageSummaryDto? usageSummary = null;
        if (subscription is not null && subscription.Status == SubscriptionStatus.Active.ToString())
            usageSummary = await usage.CurrentAsync(organizationId, ct);
        var latest = await db.Invoices.Include(x => x.Items).Where(x => x.OrganizationId == organizationId).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        var payments = await db.Payments.Include(x => x.Invoice).Include(x => x.Attempts).Where(x => x.Invoice.OrganizationId == organizationId).OrderByDescending(x => x.CreatedAt).Take(5).ToListAsync(ct);
        return new DashboardDto(org.Name, subscription, usageSummary, latest?.ToDto(), payments.Select(x => x.ToDto()).ToList());
    }
}
