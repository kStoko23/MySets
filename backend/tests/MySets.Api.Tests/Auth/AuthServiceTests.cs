using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MySets.Api.Auth;
using MySets.Api.Common;
using MySets.Api.Data;
using MySets.Api.Data.Entities;
using NSubstitute;

namespace MySets.Api.Tests.Auth;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        _userManager = Substitute.For<UserManager<User>>(
            Substitute.For<IUserStore<User>>(), null, null, null, null, null, null, null, null);

        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "mysets-tests",
            Audience = "mysets-tests-client",
            SigningKey = "unit-test-signing-key-at-least-32-bytes-long!",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 30,
        });

        _sut = new AuthService(_userManager, _dbContext, new JwtTokenGenerator(jwtOptions), jwtOptions);
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesUserAndReturnsResponse()
    {
        User? createdUser = null;
        _userManager.CreateAsync(Arg.Do<User>(u => createdUser = u), Arg.Any<string>())
            .Returns(IdentityResult.Success);

        var result = await _sut.RegisterAsync(new RegisterRequest("new@example.com", "Passw0rd!", "New User"));

        Assert.True(result.IsSuccess);
        Assert.Equal("new@example.com", result.Data!.Email);
        Assert.Equal("New User", result.Data.DisplayName);
        Assert.NotNull(createdUser);
        Assert.Equal("new@example.com", createdUser!.UserName);
        Assert.Equal("new@example.com", createdUser.Email);
    }

    [Fact]
    public async Task RegisterAsync_IdentityFailure_ReturnsValidationErrorWithIdentityMessage()
    {
        _userManager.CreateAsync(Arg.Any<User>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Email already taken." }));

        var result = await _sut.RegisterAsync(new RegisterRequest("dup@example.com", "Passw0rd!", "Dup"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorCode.ValidationError, result.Error!.Code);
        Assert.Contains("Email already taken.", result.Error.Message);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsUnauthorized()
    {
        _userManager.FindByEmailAsync("missing@example.com").Returns((User?)null);

        var result = await _sut.LoginAsync(new LoginRequest("missing@example.com", "whatever"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorCode.Unauthorized, result.Error!.Code);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsSameMessageAsUnknownEmail()
    {
        var user = await CreatePersistedUserAsync();
        _userManager.FindByEmailAsync(user.Email!).Returns(user);
        _userManager.CheckPasswordAsync(user, "wrong").Returns(false);

        var wrongPassword = await _sut.LoginAsync(new LoginRequest(user.Email!, "wrong"));
        var unknownEmail = await _sut.LoginAsync(new LoginRequest("nobody@example.com", "wrong"));

        Assert.Equal(unknownEmail.Error!.Message, wrongPassword.Error!.Message);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_IssuesTokensAndPersistsHashedRefreshToken()
    {
        var user = await CreatePersistedUserAsync();
        _userManager.FindByEmailAsync(user.Email!).Returns(user);
        _userManager.CheckPasswordAsync(user, "Passw0rd!").Returns(true);

        var result = await _sut.LoginAsync(new LoginRequest(user.Email!, "Passw0rd!"));

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Data!.User.Id);
        Assert.NotEmpty(result.Data.Tokens.AccessToken);

        var stored = await _dbContext.RefreshTokens.SingleAsync(rt => rt.UserId == user.Id);
        Assert.Null(stored.RevokedAt);

        Assert.NotEqual(result.Data.Tokens.RefreshToken, stored.TokenHash);
    }

    [Fact]
    public async Task RefreshAsync_UnknownToken_ReturnsUnauthorized()
    {
        var result = await _sut.RefreshAsync("does-not-exist");

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorCode.Unauthorized, result.Error!.Code);
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ReturnsUnauthorized()
    {
        var rawRefreshToken = await LoginAndGetRawRefreshTokenAsync();

        var stored = await _dbContext.RefreshTokens.SingleAsync();
        stored.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.RefreshAsync(rawRefreshToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorCode.Unauthorized, result.Error!.Code);
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_RevokesOldTokenAndIssuesNewOne()
    {
        var rawRefreshToken = await LoginAndGetRawRefreshTokenAsync();

        var result = await _sut.RefreshAsync(rawRefreshToken);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(rawRefreshToken, result.Data!.RefreshToken);

        var tokens = await _dbContext.RefreshTokens.ToListAsync();
        Assert.Equal(2, tokens.Count);
        Assert.Contains(tokens, t => t.RevokedAt != null);
        Assert.Contains(tokens, t => t.RevokedAt == null);
    }

    [Fact]
    public async Task RefreshAsync_TokenAlreadyUsedOnce_CannotBeReplayed()
    {
        var rawRefreshToken = await LoginAndGetRawRefreshTokenAsync();
        await _sut.RefreshAsync(rawRefreshToken);

        var replay = await _sut.RefreshAsync(rawRefreshToken);

        Assert.False(replay.IsSuccess);
        Assert.Equal(ServiceErrorCode.Unauthorized, replay.Error!.Code);
    }

    [Fact]
    public async Task LogoutAsync_ValidToken_RevokesIt()
    {
        var rawRefreshToken = await LoginAndGetRawRefreshTokenAsync();

        var result = await _sut.LogoutAsync(rawRefreshToken);

        Assert.True(result.IsSuccess);
        var stored = await _dbContext.RefreshTokens.SingleAsync();
        Assert.NotNull(stored.RevokedAt);
    }

    [Fact]
    public async Task LogoutAsync_UnknownToken_IsIdempotentAndStillSucceeds()
    {
        var result = await _sut.LogoutAsync("does-not-exist");

        Assert.True(result.IsSuccess);
    }

    private async Task<User> CreatePersistedUserAsync(
        string email = "user@example.com", string displayName = "Test User")
    {
        var user = new User { Id = Guid.NewGuid(), UserName = email, Email = email, DisplayName = displayName };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    private async Task<string> LoginAndGetRawRefreshTokenAsync()
    {
        var user = await CreatePersistedUserAsync();
        _userManager.FindByEmailAsync(user.Email!).Returns(user);
        _userManager.CheckPasswordAsync(user, "Passw0rd!").Returns(true);

        var login = await _sut.LoginAsync(new LoginRequest(user.Email!, "Passw0rd!"));
        return login.Data!.Tokens.RefreshToken;
    }
}
