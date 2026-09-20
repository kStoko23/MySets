using System.ComponentModel.DataAnnotations;

namespace MySets.Api.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required, MaxLength(100)] string DisplayName);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record UserResponse(Guid Id, string Email, string DisplayName);

public sealed record AuthTokens(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

public sealed record LoginResult(AuthTokens Tokens, UserResponse User);
