using Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Billing.Application;

public sealed class UsageService(IBillingDbContext db, IUsageCounter counter, AuditService audit)
{
    public async Task<UsagePostResponse> RecordAsync(Guid organizationId, Guid userId, UsageEventRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalEventId)) throw new ArgumentException("ExternalEventId is required.");
        if (request.Quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.Channel)) throw new ArgumentException("Channel is required.");

        var sub = await ActiveSubscription(organizationId, ct);
        var exists = await db.UsageEvents.AnyAsync(x => x.ExternalEventId == request.ExternalEventId, ct);
        if (exists)
        {
            var current = await GetCurrentUsageCount(organizationId, sub.BillingPeriodStart, sub.BillingPeriodEnd, ct);
            return new UsagePostResponse(true, current);
        }

        db.UsageEvents.Add(new UsageEvent
        {
            ExternalEventId = request.ExternalEventId,
            OrganizationId = organizationId,
            Channel = request.Channel.Trim(),
            EventType = request.EventType.Trim(),
            Quantity = request.Quantity,
            Timestamp = request.Timestamp == default ? DateTime.UtcNow : request.Timestamp.ToUniversalTime()
        });
        await db.SaveChangesAsync(ct);
        var newCount = await counter.IncrementAsync(organizationId, sub.BillingPeriodStart, request.Quantity);
        await audit.WriteAsync("UsageRecorded", nameof(UsageEvent), request.ExternalEventId, organizationId, userId, new { request.Channel, request.Quantity }, ct);
        return new UsagePostResponse(false, newCount);
    }

    public async Task<UsageSummaryDto> CurrentAsync(Guid organizationId, CancellationToken ct = default)
    {
        var sub = await ActiveSubscription(organizationId, ct);
        var used = await GetCurrentUsageCount(organizationId, sub.BillingPeriodStart, sub.BillingPeriodEnd, ct);
        var byChannel = await db.UsageEvents
            .Where(x => x.OrganizationId == organizationId && x.Timestamp >= sub.BillingPeriodStart && x.Timestamp < sub.BillingPeriodEnd)
            .GroupBy(x => x.Channel)
            .Select(g => new ChannelUsageDto(g.Key, g.Sum(x => (long)x.Quantity)))
            .OrderByDescending(x => x.Quantity)
            .ToListAsync(ct);
        var overage = Math.Max(used - sub.Plan.IncludedMessages, 0);
        var estimated = decimal.Round(overage * sub.Plan.ExtraMessagePrice, 2, MidpointRounding.AwayFromZero);
        return new UsageSummaryDto(used, sub.Plan.IncludedMessages, overage, estimated, byChannel, sub.BillingPeriodStart, sub.BillingPeriodEnd);
    }

    private async Task<long> GetCurrentUsageCount(Guid organizationId, DateTime start, DateTime end, CancellationToken ct)
    {
        var cached = await counter.GetAsync(organizationId, start);
        if (cached.HasValue) return cached.Value;
        var total = await db.UsageEvents.Where(x => x.OrganizationId == organizationId && x.Timestamp >= start && x.Timestamp < end).SumAsync(x => (long?)x.Quantity, ct) ?? 0;
        await counter.SetAsync(organizationId, start, total);
        return total;
    }

    private async Task<Subscription> ActiveSubscription(Guid organizationId, CancellationToken ct) =>
        await db.Subscriptions.Include(x => x.Plan).FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Status == SubscriptionStatus.Active, ct)
        ?? throw new InvalidOperationException("No active subscription.");
}
