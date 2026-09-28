using Billing.Application;

namespace Billing.Tests;

public sealed class BillingCalculatorTests
{
    [Fact]
    public void CalculatesBasePriceWhenUsageIsWithinPlan()
    {
        var r = BillingCalculator.CalculateInvoice(99m, 10_000, 0.01m, 8_000);
        Assert.Equal(0, r.OverageUnits);
        Assert.Equal(0m, r.OverageCost);
        Assert.Equal(99m, r.Total);
    }

    [Fact]
    public void CalculatesOverageCorrectly()
    {
        var r = BillingCalculator.CalculateInvoice(99m, 10_000, 0.01m, 12_500);
        Assert.Equal(2_500, r.OverageUnits);
        Assert.Equal(25m, r.OverageCost);
        Assert.Equal(124m, r.Total);
    }

    [Fact]
    public void CalculatesHalfPeriodUpgradeProration()
    {
        var start = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddDays(30);
        var now = start.AddDays(15);
        var r = BillingCalculator.CalculateProration(30m, 90m, start, end, now);
        Assert.Equal(30m, r);
    }
}
