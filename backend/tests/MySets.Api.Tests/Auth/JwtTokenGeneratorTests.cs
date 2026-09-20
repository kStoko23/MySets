using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using MySets.Api.Auth;
using MySets.Api.Data.Entities;

namespace MySets.Api.Tests.Auth;

public class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateAccessToken_IncludesExpectedClaimsIssuerAudienceAndExpiry()
    {
        var jwtOptions = new JwtOptions
        {
            Issuer = "mysets-tests",
            Audience = "mysets-tests-client",
            SigningKey = "unit-test-signing-key-at-least-32-bytes-long!",
            AccessTokenMinutes = 15,
        };
        var generator = new JwtTokenGenerator(Options.Create(jwtOptions));
        var user = new User { Id = Guid.NewGuid(), Email = "user@example.com", DisplayName = "Someone" };

        var (token, expiresAt) = generator.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Email, jwt.Claims.Single(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal(user.DisplayName, jwt.Claims.Single(c => c.Type == "display_name").Value);
        Assert.Equal(jwtOptions.Issuer, jwt.Issuer);
        Assert.Equal(jwtOptions.Audience, jwt.Audiences.Single());
        Assert.True(expiresAt > DateTime.UtcNow.AddMinutes(14) && expiresAt <= DateTime.UtcNow.AddMinutes(15));
    }
}
