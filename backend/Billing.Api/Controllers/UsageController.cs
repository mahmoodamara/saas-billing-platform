using Billing.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/usage")]
public sealed class UsageController(UsageService service) : ControllerBase
{
    [HttpPost("events")]
    public Task<UsagePostResponse> Record(UsageEventRequest request, CancellationToken ct) => service.RecordAsync(User.OrganizationId(), User.UserId(), request, ct);

    [HttpGet("current")]
    public Task<UsageSummaryDto> Current(CancellationToken ct) => service.CurrentAsync(User.OrganizationId(), ct);
}
