using System.Text.Json;
using Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Billing.Application;

public sealed class PaymentService(IBillingDbContext db, IPaymentProvider provider, AuditService audit)
{
    public async Task<IReadOnlyList<PaymentDto>> ListAsync(Guid organizationId, CancellationToken ct = default)
    {
        var payments = await db.Payments.Include(x => x.Invoice).Include(x => x.Attempts)
            .Where(x => x.Invoice.OrganizationId == organizationId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return payments.Select(x => x.ToDto()).ToList();
    }

    public async Task<PaymentDto> PayInvoiceAsync(Guid organizationId, Guid userId, Guid invoiceId, string idempotencyKey, bool simulateFailure, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Idempotency-Key header is required.");
        var operation = $"pay-invoice:{invoiceId}";
        var previous = await db.IdempotencyRecords.FirstOrDefaultAsync(x => x.Key == idempotencyKey && x.Operation == operation, ct);
        if (previous is not null) return JsonSerializer.Deserialize<PaymentDto>(previous.ResponseJson)!;

        var invoice = await db.Invoices.Include(x => x.Payments).FirstOrDefaultAsync(x => x.Id == invoiceId && x.OrganizationId == organizationId, ct) ?? throw new KeyNotFoundException("Invoice not found.");
        if (invoice.Status == InvoiceStatus.Paid)
        {
            var successful = await db.Payments.Include(x => x.Attempts).FirstOrDefaultAsync(x => x.InvoiceId == invoiceId && x.Status == PaymentStatus.Succeeded, ct);
            if (successful is not null) return successful.ToDto();
            throw new InvalidOperationException("Invoice is already paid.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var payment = new Payment { InvoiceId = invoice.Id, Amount = invoice.Total, Currency = "USD", IdempotencyKey = idempotencyKey, Status = PaymentStatus.Pending };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        var result = await provider.ChargeAsync(payment.Amount, payment.Currency, idempotencyKey, simulateFailure, ct);
        payment.ProviderReference = result.ProviderReference;
        payment.Status = result.Success ? PaymentStatus.Succeeded : PaymentStatus.Failed;
        payment.UpdatedAt = DateTime.UtcNow;
        payment.Attempts.Add(new PaymentAttempt { AttemptNumber = 1, Status = payment.Status, Error = result.Error });
        invoice.Status = result.Success ? InvoiceStatus.Paid : InvoiceStatus.PastDue;
        var dto = payment.ToDto();
        db.IdempotencyRecords.Add(new IdempotencyRecord { Key = idempotencyKey, Operation = operation, ResponseJson = JsonSerializer.Serialize(dto) });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await audit.WriteAsync(result.Success ? "PaymentSucceeded" : "PaymentFailed", nameof(Payment), payment.Id.ToString(), organizationId, userId, new { payment.Amount, result.Error }, ct);
        return payment.ToDto();
    }

    public async Task<RetryPaymentResponse> RetryAsync(Guid paymentId, Guid userId, CancellationToken ct = default)
    {
        var payment = await db.Payments.Include(x => x.Invoice).Include(x => x.Attempts).FirstOrDefaultAsync(x => x.Id == paymentId, ct) ?? throw new KeyNotFoundException("Payment not found.");
        if (payment.Status != PaymentStatus.Failed) return new RetryPaymentResponse(payment.Id, payment.Status.ToString(), payment.Attempts.Count);
        if (payment.Attempts.Count >= 3) throw new InvalidOperationException("Maximum retry attempts reached.");

        var result = await provider.ChargeAsync(payment.Amount, payment.Currency, payment.IdempotencyKey + $":retry:{payment.Attempts.Count + 1}", false, ct);
        payment.ProviderReference = result.ProviderReference;
        payment.Status = result.Success ? PaymentStatus.Succeeded : PaymentStatus.Failed;
        payment.UpdatedAt = DateTime.UtcNow;
        payment.Attempts.Add(new PaymentAttempt { AttemptNumber = payment.Attempts.Count + 1, Status = payment.Status, Error = result.Error });
        payment.Invoice.Status = result.Success ? InvoiceStatus.Paid : InvoiceStatus.PastDue;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(result.Success ? "PaymentRetrySucceeded" : "PaymentRetryFailed", nameof(Payment), payment.Id.ToString(), payment.Invoice.OrganizationId, userId, new { attempts = payment.Attempts.Count }, ct);
        return new RetryPaymentResponse(payment.Id, payment.Status.ToString(), payment.Attempts.Count);
    }

    public async Task<PaymentDto> RefundAsync(Guid paymentId, decimal amount, string reason, Guid userId, CancellationToken ct = default)
    {
        if (amount <= 0) throw new ArgumentException("Refund amount must be positive.");
        var payment = await db.Payments.Include(x => x.Invoice).Include(x => x.Attempts).Include(x => x.Refunds).FirstOrDefaultAsync(x => x.Id == paymentId, ct) ?? throw new KeyNotFoundException("Payment not found.");
        if (payment.Status is not (PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded)) throw new InvalidOperationException("Only successful payments can be refunded.");
        var alreadyRefunded = payment.Refunds.Where(x => x.Status == RefundStatus.Succeeded).Sum(x => x.Amount);
        var refundable = payment.Amount - alreadyRefunded;
        if (amount > refundable) throw new InvalidOperationException($"Refund exceeds remaining refundable amount ({refundable:C}).");

        var result = await provider.RefundAsync(payment.ProviderReference, amount, ct);
        var refund = new Refund { PaymentId = payment.Id, Amount = amount, Reason = reason, Status = result.Success ? RefundStatus.Succeeded : RefundStatus.Failed };
        payment.Refunds.Add(refund);
        if (result.Success)
        {
            var totalRefunded = alreadyRefunded + amount;
            payment.Status = totalRefunded >= payment.Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
        }
        payment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(result.Success ? "RefundSucceeded" : "RefundFailed", nameof(Refund), refund.Id.ToString(), payment.Invoice.OrganizationId, userId, new { amount, reason }, ct);
        return payment.ToDto();
    }
}
