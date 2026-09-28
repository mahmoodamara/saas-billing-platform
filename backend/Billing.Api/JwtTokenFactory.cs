using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Billing.Domain;
using Microsoft.IdentityModel.Tokens;

namespace Billing.Api;

public sealed class JwtTokenFactory(IConfiguration config)
{
    public string Create(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        if (user.OrganizationId.HasValue) claims.Add(new("organizationId", user.OrganizationId.Value.ToString()));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
