using Billing.Application;
using Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure;

public static class SeedData
{
    public static async Task InitializeAsync(BillingDbContext db, CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);
        if (await db.Users.AnyAsync(ct)) return;

        var starter = new Plan { Name = "Starter", MonthlyPrice = 29m, IncludedMessages = 2_000, ExtraMessagePrice = 0.015m };
        var pro = new Plan { Name = "Pro", MonthlyPrice = 99m, IncludedMessages = 10_000, ExtraMessagePrice = 0.01m };
        var business = new Plan { Name = "Business", MonthlyPrice = 299m, IncludedMessages = 50_000, ExtraMessagePrice = 0.007m };
        db.Plans.AddRange(starter, pro, business);

        var org = new Organization { Name = "Acme Messaging" };
        db.Organizations.Add(org);
        var admin = new User { Email = "admin@demo.local", PasswordHash = PasswordHasher.Hash("Admin123!"), Role = UserRole.Admin, Organization = org };
        var customer = new User { Email = "customer@demo.local", PasswordHash = PasswordHasher.Hash("Customer123!"), Role = UserRole.Customer, Organization = org };
        db.Users.AddRange(admin, customer);

        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var sub = new Subscription { Organization = org, Plan = pro, Status = SubscriptionStatus.Active, BillingPeriodStart = start, BillingPeriodEnd = start.AddMonths(1) };
        db.Subscriptions.Add(sub);

        db.UsageEvents.AddRange(
            new UsageEvent { ExternalEventId = "seed-whatsapp", Organization = org, Channel = "WhatsApp", EventType = "Message", Quantity = 8_500, Timestamp = start.AddDays(3) },
            new UsageEvent { ExternalEventId = "seed-instagram", Organization = org, Channel = "Instagram", EventType = "Message", Quantity = 2_000, Timestamp = start.AddDays(5) },
            new UsageEvent { ExternalEventId = "seed-facebook", Organization = org, Channel = "Facebook", EventType = "Message", Quantity = 2_000, Timestamp = start.AddDays(7) }
        );

        await db.SaveChangesAsync(ct);
    }
}
