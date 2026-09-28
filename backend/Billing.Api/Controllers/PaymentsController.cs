using Billing.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
public sealed class PaymentsController(PaymentService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PaymentDto>> List(CancellationToken ct) => service.ListAsync(User.OrganizationId(), ct);

    [HttpPost("{id:guid}/refund")]
    [Authorize(Roles = "Admin")]
    public Task<PaymentDto> Refund(Guid id, RefundRequest request, CancellationToken ct) => service.RefundAsync(id, request.Amount, request.Reason, User.UserId(), ct);

    [HttpPost("{id:guid}/retry")]
    [Authorize(Roles = "Admin")]
    public Task<RetryPaymentResponse> Retry(Guid id, CancellationToken ct) => service.RetryAsync(id, User.UserId(), ct);
}
