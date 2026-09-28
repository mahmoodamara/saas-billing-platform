using Billing.Application;
using Billing.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Billing.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(BillingDbContext db, IConfiguration config) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == request.Email, ct);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash)) return Unauthorized(new { title = "Invalid email or password." });
        var token = new JwtTokenFactory(config).Create(user);
        return Ok(new LoginResponse(token, user.Email, user.Role.ToString(), user.OrganizationId));
    }
}
