using Billing.Application;

namespace Billing.Api;

public sealed class BillingCycleWorker(IServiceScopeFactory scopeFactory, ILogger<BillingCycleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<BillingCycleService>();
                var processed = await service.ProcessDueAsync(stoppingToken);
                if (processed > 0) logger.LogInformation("Advanced {Count} billing cycle(s)", processed);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Billing-cycle worker iteration failed");
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
