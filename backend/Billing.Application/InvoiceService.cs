using Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Billing.Application;

public sealed class InvoiceService(IBillingDbContext db, UsageService usageService, AuditService audit)
{
    public async Task<IReadOnlyList<InvoiceDto>> ListAsync(Guid organizationId, CancellationToken ct = default)
    {
        var invoices = await db.Invoices.Include(x => x.Items).Where(x => x.OrganizationId == organizationId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return invoices.Select(x => x.ToDto()).ToList();
    }

    public async Task<InvoiceDto> GetAsync(Guid organizationId, Guid id, CancellationToken ct = default)
    {
        var invoice = await db.Invoices.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId, ct) ?? throw new KeyNotFoundException("Invoice not found.");
        return invoice.ToDto();
    }

    public async Task<InvoiceDto> GenerateCurrentAsync(Guid organizationId, Guid userId, CancellationToken ct = default)
    {
        var sub = await db.Subscriptions.Include(x => x.Plan).FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Status == SubscriptionStatus.Active, ct) ?? throw new InvalidOperationException("No active subscription.");
        var existing = await db.Invoices.Include(x => x.Items).FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.SubscriptionId == sub.Id && x.PeriodStart == sub.BillingPeriodStart && x.PeriodEnd == sub.BillingPeriodEnd, ct);
        if (existing is not null) return existing.ToDto();

        var usage = await usageService.CurrentAsync(organizationId, ct);
        var calculation = BillingCalculator.CalculateInvoice(sub.Plan.MonthlyPrice, sub.Plan.IncludedMessages, sub.Plan.ExtraMessagePrice, usage.Used);
        var invoice = new Invoice
        {
            OrganizationId = organizationId,
            SubscriptionId = sub.Id,
            Status = InvoiceStatus.Open,
            PeriodStart = sub.BillingPeriodStart,
            PeriodEnd = sub.BillingPeriodEnd,
            Subtotal = calculation.Total,
            Total = calculation.Total,
            DueAt = DateTime.UtcNow.AddDays(7),
            Items = [
                new InvoiceItem { Type = InvoiceItemType.Subscription, Description = $"{sub.Plan.Name} monthly subscription", Quantity = 1, UnitPrice = sub.Plan.MonthlyPrice, Amount = sub.Plan.MonthlyPrice }
            ]
        };
        if (calculation.OverageUnits > 0)
        {
            invoice.Items.Add(new InvoiceItem { Type = InvoiceItemType.Overage, Description = $"Extra usage: {calculation.OverageUnits:N0} messages", Quantity = calculation.OverageUnits, UnitPrice = sub.Plan.ExtraMessagePrice, Amount = calculation.OverageCost });
        }
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("InvoiceGenerated", nameof(Invoice), invoice.Id.ToString(), organizationId, userId, new { invoice.Total, usage = usage.Used }, ct);
        return invoice.ToDto();
    }
}
