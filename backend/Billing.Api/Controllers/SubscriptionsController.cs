using Billing.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/subscriptions")]
public sealed class SubscriptionsController(SubscriptionService service) : ControllerBase
{
    [HttpGet("current")]
    public async Task<ActionResult<SubscriptionDto>> Current(CancellationToken ct)
    {
        var result = await service.GetCurrentAsync(User.OrganizationId(), ct); return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public Task<SubscriptionDto> Create(SubscriptionRequest request, CancellationToken ct) => service.CreateAsync(User.OrganizationId(), request.PlanId, User.UserId(), ct);

    [HttpPost("{id:guid}/upgrade")]
    public async Task<IActionResult> Upgrade(Guid id, ChangePlanRequest request, CancellationToken ct)
    {
        var result = await service.UpgradeAsync(User.OrganizationId(), id, request.PlanId, User.UserId(), ct); return Ok(new { subscription = result.Subscription, prorationInvoice = result.ProrationInvoice });
    }

    [HttpPost("{id:guid}/downgrade")]
    public Task<SubscriptionDto> Downgrade(Guid id, ChangePlanRequest request, CancellationToken ct) => service.DowngradeAsync(User.OrganizationId(), id, request.PlanId, User.UserId(), ct);

    [HttpPost("{id:guid}/cancel")]
    public Task<SubscriptionDto> Cancel(Guid id, CancellationToken ct) => service.CancelAsync(User.OrganizationId(), id, User.UserId(), ct);
}
