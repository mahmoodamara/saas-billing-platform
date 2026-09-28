using System.Security.Claims;

namespace Billing.Api;

public static class ClaimsExtensions
{
    public static Guid UserId(this ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException());
    public static Guid OrganizationId(this ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue("organizationId") ?? throw new UnauthorizedAccessException("User has no organization."));
}
