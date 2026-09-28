using System.Net.Http.Json;
using System.Text.Json;
using Billing.Application;
using Billing.Domain;

namespace Billing.Infrastructure;

public sealed class ElasticAuditIndexer(HttpClient http) : IAuditIndexer
{
    private const string IndexName = "billing-audit";

    public async Task IndexAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await http.PutAsJsonAsync($"{IndexName}/_doc/{log.Id}", log, cancellationToken);
            // SQL is authoritative. Search indexing is best-effort by design.
        }
        catch
        {
            // Ignore search-index failures so they never fail a billing transaction.
        }
    }

    public async Task<IReadOnlyList<AuditSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new
            {
                size = 50,
                query = new
                {
                    multi_match = new
                    {
                        query,
                        fields = new[] { "action", "entityType", "entityId", "dataJson" }
                    }
                }
            };
            using var response = await http.PostAsJsonAsync($"{IndexName}/_search", body, cancellationToken);
            if (!response.IsSuccessStatusCode) return [];
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var hits = doc.RootElement.GetProperty("hits").GetProperty("hits");
            var result = new List<AuditSearchResult>();
            foreach (var hit in hits.EnumerateArray())
            {
                var src = hit.GetProperty("_source");
                result.Add(new AuditSearchResult(
                    src.GetProperty("action").GetString() ?? "",
                    src.GetProperty("entityType").GetString() ?? "",
                    src.GetProperty("entityId").GetString() ?? "",
                    src.GetProperty("dataJson").GetString() ?? "{}",
                    src.GetProperty("createdAt").GetDateTime()));
            }
            return result;
        }
        catch
        {
            return [];
        }
    }
}
