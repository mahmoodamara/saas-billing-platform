namespace Billing.Application;

public sealed record InvoiceCalculation(decimal BasePrice, long OverageUnits, decimal OverageCost, decimal Total);

public static class BillingCalculator
{
    public static InvoiceCalculation CalculateInvoice(decimal monthlyPrice, int includedUnits, decimal extraUnitPrice, long actualUsage)
    {
        if (monthlyPrice < 0) throw new ArgumentOutOfRangeException(nameof(monthlyPrice));
        if (includedUnits < 0) throw new ArgumentOutOfRangeException(nameof(includedUnits));
        if (extraUnitPrice < 0) throw new ArgumentOutOfRangeException(nameof(extraUnitPrice));
        if (actualUsage < 0) throw new ArgumentOutOfRangeException(nameof(actualUsage));

        var overage = Math.Max(actualUsage - includedUnits, 0);
        var overageCost = decimal.Round(overage * extraUnitPrice, 2, MidpointRounding.AwayFromZero);
        var total = decimal.Round(monthlyPrice + overageCost, 2, MidpointRounding.AwayFromZero);
        return new InvoiceCalculation(monthlyPrice, overage, overageCost, total);
    }

    public static decimal CalculateProration(decimal oldMonthlyPrice, decimal newMonthlyPrice, DateTime periodStart, DateTime periodEnd, DateTime now)
    {
        if (newMonthlyPrice <= oldMonthlyPrice) return 0m;
        var totalSeconds = (periodEnd - periodStart).TotalSeconds;
        if (totalSeconds <= 0) throw new ArgumentException("Billing period must be positive.");
        var remainingSeconds = Math.Clamp((periodEnd - now).TotalSeconds, 0, totalSeconds);
        var remainingRatio = (decimal)(remainingSeconds / totalSeconds);
        return decimal.Round((newMonthlyPrice - oldMonthlyPrice) * remainingRatio, 2, MidpointRounding.AwayFromZero);
    }
}
