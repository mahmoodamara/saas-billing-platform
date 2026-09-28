using Billing.Application;

namespace Billing.Infrastructure;

public sealed class MockPaymentProvider : IPaymentProvider
{
    public Task<ProviderChargeResult> ChargeAsync(decimal amount, string currency, string idempotencyKey, bool simulateFailure, CancellationToken cancellationToken)
    {
        if (simulateFailure)
            return Task.FromResult(new ProviderChargeResult(false, $"mock_failed_{Guid.NewGuid():N}", "Simulated provider decline."));
        return Task.FromResult(new ProviderChargeResult(true, $"mock_charge_{Guid.NewGuid():N}"));
    }

    public Task<ProviderChargeResult> RefundAsync(string providerReference, decimal amount, CancellationToken cancellationToken) =>
        Task.FromResult(new ProviderChargeResult(true, $"mock_refund_{Guid.NewGuid():N}"));
}
