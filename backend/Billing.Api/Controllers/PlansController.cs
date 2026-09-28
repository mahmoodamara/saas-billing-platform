using Billing.Application;
using Billing.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Billing.Api.Controllers;

[ApiController]
[Route("api/plans")]
public sealed class PlansController(IBillingDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IReadOnlyList<PlanDto>> Get(CancellationToken ct) => (await db.Plans.OrderBy(x => x.MonthlyPrice).ToListAsync(ct)).Select(x => x.ToDto()).ToList();

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PlanDto>> Create(PlanRequest request, CancellationToken ct)
    {
        if (request.MonthlyPrice < 0 || request.IncludedMessages < 0 || request.ExtraMessagePrice < 0) return BadRequest();
        var plan = new Plan { Name = request.Name, MonthlyPrice = request.MonthlyPrice, IncludedMessages = request.IncludedMessages, ExtraMessagePrice = request.ExtraMessagePrice, IsActive = request.IsActive };
        db.Plans.Add(plan); await db.SaveChangesAsync(ct); return Ok(plan.ToDto());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PlanDto>> Update(Guid id, PlanRequest request, CancellationToken ct)
    {
        var plan = await db.Plans.FirstOrDefaultAsync(x => x.Id == id, ct); if (plan is null) return NotFound();
        plan.Name = request.Name; plan.MonthlyPrice = request.MonthlyPrice; plan.IncludedMessages = request.IncludedMessages; plan.ExtraMessagePrice = request.ExtraMessagePrice; plan.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct); return Ok(plan.ToDto());
    }
}
