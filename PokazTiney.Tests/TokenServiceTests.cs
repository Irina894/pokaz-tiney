using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PokazTiney.Api.Models;
using PokazTiney.Api.Services;

namespace PokazTiney.Tests;

public class TokenServiceTests
{
    private static JwtOptions Options() => new() { Key = new string('k', 40), ExpiresMinutes = 60 };

    private static TokenValidationParameters Validation(JwtOptions o) => new()
    {
        ValidIssuer = o.Issuer,
        ValidAudience = o.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Key)),
        RoleClaimType = "role"
    };

    [Fact]
    public async Task Create_ProducesValidTokenWithUserIdAndRole()
    {
        var options = Options();
        var user = new User { Id = 7, Email = "admin@example.com", Role = UserRole.Admin };

        var token = new TokenService(options).Create(user);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, Validation(options));

        Assert.True(result.IsValid);
        Assert.Equal("7", result.ClaimsIdentity.FindFirst("sub")?.Value);
        Assert.Equal("Admin", result.ClaimsIdentity.FindFirst("role")?.Value);
        Assert.Equal("admin@example.com", result.ClaimsIdentity.FindFirst("email")?.Value);
    }

    [Fact]
    public async Task Token_SignedWithOtherKey_IsRejected()
    {
        var options = Options();
        var token = new TokenService(options).Create(new User { Id = 1, Email = "a@b.com" });

        var other = Options();
        other.Key = new string('x', 40);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, Validation(other));

        Assert.False(result.IsValid);
    }
}
