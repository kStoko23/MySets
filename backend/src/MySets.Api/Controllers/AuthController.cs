using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySets.Api.Auth;
using MySets.Api.Common;

namespace MySets.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService, IWebHostEnvironment environment) : ControllerBase
{
    private const string AccessTokenCookie = "access_token";
    private const string RefreshTokenCookie = "refresh_token";

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return this.ToActionResult(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request);

        if (!result.IsSuccess)
            return this.ToActionResult(result);

        SetAuthCookies(result.Data!.Tokens);
        return Ok(result.Data.User);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh()
    {
        var rawRefreshToken = Request.Cookies[RefreshTokenCookie];

        if (string.IsNullOrEmpty(rawRefreshToken))
            return Unauthorized();

        var result = await authService.RefreshAsync(rawRefreshToken);

        if (!result.IsSuccess)
        {
            ClearAuthCookies();
            return this.ToActionResult(result);
        }

        SetAuthCookies(result.Data!);
        return NoContent();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var rawRefreshToken = Request.Cookies[RefreshTokenCookie];

        if (!string.IsNullOrEmpty(rawRefreshToken))
            await authService.LogoutAsync(rawRefreshToken);

        ClearAuthCookies();
        return NoContent();
    }

    private void SetAuthCookies(AuthTokens tokens)
    {
        var secure = !environment.IsDevelopment();

        Response.Cookies.Append(AccessTokenCookie, tokens.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Expires = tokens.AccessTokenExpiresAt,
            Path = "/",
        });

        Response.Cookies.Append(RefreshTokenCookie, tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Expires = tokens.RefreshTokenExpiresAt,
            Path = "/api/auth",
        });
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete(AccessTokenCookie, new CookieOptions { Path = "/" });
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions { Path = "/api/auth" });
    }
}
