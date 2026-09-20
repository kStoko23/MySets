using System.Security.Claims;
using MySets.Api.Common;

namespace MySets.Api.Tests.Common;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_ValidClaim_ReturnsGuid()
    {
        var userId = Guid.NewGuid();
        var principal = CreatePrincipal(userId.ToString());

        var result = principal.GetUserId();

        Assert.Equal(userId, result);
    }

    [Fact]
    public void GetUserId_MissingClaim_ThrowsUnauthorizedAccessException()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Throws<UnauthorizedAccessException>(() => principal.GetUserId());
    }

    [Fact]
    public void GetUserId_NonGuidClaim_ThrowsUnauthorizedAccessException()
    {
        var principal = CreatePrincipal("not-a-guid");

        Assert.Throws<UnauthorizedAccessException>(() => principal.GetUserId());
    }

    private static ClaimsPrincipal CreatePrincipal(string nameIdentifierValue)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, nameIdentifierValue)]);
        return new ClaimsPrincipal(identity);
    }
}
