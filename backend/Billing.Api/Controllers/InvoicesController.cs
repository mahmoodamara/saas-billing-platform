using Billing.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/invoices")]
public sealed class InvoicesController(InvoiceService invoices, PaymentService payments) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<InvoiceDto>> List(CancellationToken ct) => invoices.ListAsync(User.OrganizationId(), ct);

    [HttpGet("{id:guid}")]
    public Task<InvoiceDto> Get(Guid id, CancellationToken ct) => invoices.GetAsync(User.OrganizationId(), id, ct);

    [HttpPost("generate-current")]
    public Task<InvoiceDto> Generate(CancellationToken ct) => invoices.GenerateCurrentAsync(User.OrganizationId(), User.UserId(), ct);

    [HttpPost("{id:guid}/pay")]
    public Task<PaymentDto> Pay(Guid id, PayInvoiceRequest request, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken ct) => payments.PayInvoiceAsync(User.OrganizationId(), User.UserId(), id, idempotencyKey, request.SimulateFailure, ct);
}
