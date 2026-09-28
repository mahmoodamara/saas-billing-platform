using Billing.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/audit")]
public sealed class AuditController(IAuditIndexer indexer) : ControllerBase
{
    [HttpGet("search")]
    public Task<IReadOnlyList<AuditSearchResult>> Search([FromQuery] string q, CancellationToken ct) => indexer.SearchAsync(q, ct);
}
