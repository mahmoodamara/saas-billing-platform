using Billing.Application;
using Billing.Domain;
using Billing.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Billing.Api;

public sealed class PaymentRetryWorker(IServiceScopeFactory scopeFactory, ILogger<PaymentRetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
                var service = scope.ServiceProvider.GetRequiredService<PaymentService>();
                var retryable = await db.Payments.Include(x => x.Attempts).Where(x => x.Status == PaymentStatus.Failed && x.Attempts.Count < 3).Select(x => x.Id).Take(20).ToListAsync(stoppingToken);
                foreach (var id in retryable)
                {
                    try { await service.RetryAsync(id, Guid.Empty, stoppingToken); }
                    catch (Exception ex) { logger.LogWarning(ex, "Automatic retry failed for payment {PaymentId}", id); }
                }
            }
            catch (Exception ex) { logger.LogError(ex, "Payment retry worker iteration failed"); }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
