using Billing.Domain;

namespace Billing.Application;

public static class Mapping
{
    public static PlanDto ToDto(this Plan p) => new(p.Id, p.Name, p.MonthlyPrice, p.IncludedMessages, p.ExtraMessagePrice, p.IsActive);

    public static SubscriptionDto ToDto(this Subscription s) => new(
        s.Id,
        s.Status.ToString(),
        s.Plan.ToDto(),
        s.ScheduledPlan?.ToDto(),
        s.BillingPeriodStart,
        s.BillingPeriodEnd);

    public static InvoiceDto ToDto(this Invoice i) => new(
        i.Id,
        i.Status.ToString(),
        i.PeriodStart,
        i.PeriodEnd,
        i.Total,
        i.DueAt,
        i.Items.OrderBy(x => x.CreatedAt).Select(x => new InvoiceItemDto(x.Id, x.Type.ToString(), x.Description, x.Quantity, x.UnitPrice, x.Amount)).ToList());

    public static PaymentDto ToDto(this Payment p) => new(
        p.Id,
        p.InvoiceId,
        p.Amount,
        p.Currency,
        p.Status.ToString(),
        p.ProviderReference,
        p.Attempts.Count,
        p.CreatedAt);
}
