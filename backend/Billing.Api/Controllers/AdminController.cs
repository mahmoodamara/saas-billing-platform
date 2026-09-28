using Billing.Application;
using Billing.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Billing.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public sealed class AdminController(BillingDbContext db, PaymentService payments) : ControllerBase
{
    [HttpGet("payments")]
    public async Task<IReadOnlyList<AdminPaymentDto>> Payments(CancellationToken ct)
    {
        var rows = await db.Payments
            .Include(x => x.Invoice).ThenInclude(x => x.Organization)
            .Include(x => x.Attempts)
            .Include(x => x.Refunds)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(ct);
        return rows.Select(x => new AdminPaymentDto(
            x.Id,
            x.Invoice.Organization.Name,
            x.InvoiceId,
            x.Amount,
            x.Currency,
            x.Status.ToString(),
            x.Attempts.Count,
            x.Refunds.Where(r => r.Status == Billing.Domain.RefundStatus.Succeeded).Sum(r => r.Amount),
            x.CreatedAt)).ToList();
    }

    [HttpPost("payments/{id:guid}/retry")]
    public Task<RetryPaymentResponse> Retry(Guid id, CancellationToken ct) => payments.RetryAsync(id, User.UserId(), ct);

    [HttpPost("payments/{id:guid}/refund")]
    public Task<PaymentDto> Refund(Guid id, RefundRequest request, CancellationToken ct) => payments.RefundAsync(id, request.Amount, request.Reason, User.UserId(), ct);
}
