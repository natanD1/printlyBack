using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Printly.Domain.Entities;
using Printly.Infrastructure.Security;

namespace Printly.Application.Tests;

public class TokenServiceTests
{
    private static readonly JwtSettings Settings = new()
    {
        Secret = "test-secret-with-at-least-32-characters!!",
        Issuer = "issuer",
        Audience = "audience"
    };

    private static TokenService Create(FakeTimeProvider? time = null) =>
        new(Options.Create(Settings), time ?? new FakeTimeProvider());

    private static User NewUser(bool admin = false)
    {
        var user = new User("Fulano", "a@b.com", "hash", Guid.NewGuid(), Guid.NewGuid());
        if (admin)
            typeof(User).GetProperty(nameof(User.IsAdmin))!.SetValue(user, true);
        return user;
    }

    [Fact]
    public void Defaults_Are15MinutesAnd7Days()
    {
        var settings = new JwtSettings();

        Assert.Equal(15, settings.AccessTokenMinutes);
        Assert.Equal(7, settings.RefreshTokenDays);
        Assert.Equal(TimeSpan.FromDays(7), Create().RefreshTokenLifetime);
    }

    private static readonly string[] StandardTypes = { "sub", "jti", "exp", "nbf", "iat", "iss", "aud" };

    [Fact]
    public void AccessToken_ForRegularUser_HasOnlySubAndJti_NoRoleEmailOrName()
    {
        var user = NewUser();

        var (token, expiresIn) = Create().GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(900, expiresIn);
        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Empty(jwt.Claims.Select(c => c.Type).Except(StandardTypes));
        Assert.Contains(jwt.Claims, c => c.Type == "jti" && Guid.TryParse(c.Value, out _));
    }

    [Fact]
    public void AccessToken_ForAdmin_AddsOnlyRoleAdmin()
    {
        var (token, _) = Create().GenerateAccessToken(NewUser(admin: true));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var extra = Assert.Single(jwt.Claims.Where(c => !StandardTypes.Contains(c.Type)));
        Assert.Equal(ClaimTypes.Role, extra.Type);
        Assert.Equal("Admin", extra.Value);
    }

    [Fact]
    public void AccessToken_Expires15MinutesAfterNow_AndValidatesWithSettings()
    {
        var time = new FakeTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        var (token, _) = Create(time).GenerateAccessToken(NewUser());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(time.UtcNow.UtcDateTime.AddMinutes(15), jwt.ValidTo, TimeSpan.FromSeconds(1));

        new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            ValidIssuer = Settings.Issuer,
            ValidAudience = Settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Settings.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        }, out _);
    }

    [Fact]
    public void RefreshToken_IsOpaqueBase64Url43Chars_AndUnique()
    {
        var service = Create();

        var tokens = Enumerable.Range(0, 50).Select(_ => service.GenerateRefreshToken()).ToList();

        Assert.Equal(50, tokens.Distinct().Count());
        Assert.All(tokens, t =>
        {
            Assert.Equal(43, t.Length);
            Assert.Matches("^[A-Za-z0-9_-]{43}$", t);
        });
    }

    [Fact]
    public void HashRefreshToken_IsDeterministicLowercaseHex64_AndDiffersPerToken()
    {
        var service = Create();

        var hash = service.HashRefreshToken("abc");

        Assert.Equal(hash, service.HashRefreshToken("abc"));
        Assert.Matches("^[0-9a-f]{64}$", hash);
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", hash);
        Assert.NotEqual(hash, service.HashRefreshToken("abd"));
    }
}
