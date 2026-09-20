using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MySets.Api.Common;
using MySets.Api.Data;
using MySets.Api.Data.Entities;

namespace MySets.Api.Auth;

public class AuthService(
    UserManager<User> userManager,
    AppDbContext dbContext,
    JwtTokenGenerator tokenGenerator,
    IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<ServiceResult<UserResponse>> RegisterAsync(RegisterRequest request)
    {
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return ServiceError.ValidationError(string.Join(" ", result.Errors.Select(e => e.Description)));

        return new UserResponse(user.Id, user.Email!, user.DisplayName);
    }

    public async Task<ServiceResult<LoginResult>> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return ServiceError.Unauthorized("Invalid email or password.");

        var tokens = await IssueTokensAsync(user);
        return new LoginResult(tokens, new UserResponse(user.Id, user.Email!, user.DisplayName));
    }

    public async Task<ServiceResult<AuthTokens>> RefreshAsync(string rawRefreshToken)
    {
        var tokenHash = Hash(rawRefreshToken);
        var now = DateTime.UtcNow;

        var existing = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (existing is null || existing.RevokedAt is not null || existing.ExpiresAt <= now)
            return ServiceError.Unauthorized("Invalid refresh token.");

        existing.RevokedAt = now;

        return await IssueTokensAsync(existing.User);
    }

    public async Task<ServiceResult> LogoutAsync(string rawRefreshToken)
    {
        var tokenHash = Hash(rawRefreshToken);

        var existing = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.RevokedAt == null);

        if (existing is not null)
        {
            existing.RevokedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        return ServiceResult.Success();
    }

    private async Task<AuthTokens> IssueTokensAsync(User user)
    {
        var (accessToken, accessExpiresAt) = tokenGenerator.GenerateAccessToken(user);

        var rawRefreshToken = GenerateRefreshTokenValue();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(rawRefreshToken),
            ExpiresAt = refreshExpiresAt,
        });

        await dbContext.SaveChangesAsync();

        return new AuthTokens(accessToken, accessExpiresAt, rawRefreshToken, refreshExpiresAt);
    }

    private static string GenerateRefreshTokenValue() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string Hash(string value) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
