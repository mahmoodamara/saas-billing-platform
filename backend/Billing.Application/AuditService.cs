using System.Text.Json;
using Billing.Domain;

namespace Billing.Application;

public sealed class AuditService(IBillingDbContext db, IAuditIndexer indexer)
{
    public async Task WriteAsync(string action, string entityType, string entityId, Guid? organizationId, Guid? userId, object? data = null, CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OrganizationId = organizationId,
            UserId = userId,
            DataJson = JsonSerializer.Serialize(data ?? new { })
        };
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(ct);
        await indexer.IndexAsync(log, ct);
    }
}
