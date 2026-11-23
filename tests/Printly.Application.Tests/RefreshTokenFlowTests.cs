using Printly.Application.DTOs.Auth;
using Printly.Application.Services;
using Printly.Domain.Entities;
using Printly.Domain.Exceptions;

namespace Printly.Application.Tests;

public class RefreshTokenFlowTests
{
    private const string StrongPassword = "Abcdef1!";

    private sealed class Context
    {
        public FakeUserRepository Users { get; } = new();
        public FakeInviteCodeRepository Invites { get; } = new();
        public FakeRefreshTokenRepository RefreshTokens { get; } = new();
        public FakeTokenService Tokens { get; } = new();
        public FakeTimeProvider Time { get; } = new();
        public AuthService Service { get; }

        public Context()
        {
            Service = new AuthService(Users, Invites, new FakePasswordHasher(), Tokens, RefreshTokens, new FakeUnitOfWork(), Time);
        }

        public async Task<AuthResponse> RegisterAsync()
        {
            Invites.Items.Add(new InviteCode("ABC123", null));
            return await Service.RegisterAsync(new RegisterRequest("Fulano", "a@b.com", StrongPassword, "ABC123"));
        }

        public RefreshToken Stored(string rawToken) =>
            RefreshTokens.Items.Single(t => t.TokenHash == Tokens.HashRefreshToken(rawToken));
    }

    [Fact]
    public async Task Refresh_RotatesToken_InSameFamily_AndSlidesExpiry()
    {
        var ctx = new Context();
        var first = await ctx.RegisterAsync();
        ctx.Time.Advance(TimeSpan.FromDays(3));

        var second = await ctx.Service.RefreshAsync(first.RefreshToken);

        var old = ctx.Stored(first.RefreshToken);
        var current = ctx.Stored(second.RefreshToken);
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.True(old.Revoked);
        Assert.Equal(ctx.Time.UtcNow.UtcDateTime, old.RevokedAt);
        Assert.False(current.Revoked);
        Assert.Equal(old.FamilyId, current.FamilyId);
        Assert.Equal(ctx.Time.UtcNow.UtcDateTime.AddDays(7), current.ExpiresAt);
        Assert.Equal(first.User.Id, second.User.Id);
        Assert.Equal(900, second.AccessTokenExpiresIn);
        Assert.Equal(604800, second.RefreshTokenExpiresIn);
        Assert.Equal(2, ctx.RefreshTokens.Items.Count);
    }

    [Fact]
    public async Task Refresh_ReusingRotatedToken_RevokesWholeFamily_AndThrowsUnauthorized()
    {
        var ctx = new Context();
        var first = await ctx.RegisterAsync();
        var second = await ctx.Service.RefreshAsync(first.RefreshToken);
        var third = await ctx.Service.RefreshAsync(second.RefreshToken);

        // Atacante (ou aba velha) reapresenta o token ja rotacionado.
        await Assert.ThrowsAsync<UnauthorizedException>(() => ctx.Service.RefreshAsync(first.RefreshToken));

        Assert.All(ctx.RefreshTokens.Items, t => Assert.True(t.Revoked));
        Assert.True(ctx.Stored(third.RefreshToken).Revoked);
        await Assert.ThrowsAsync<UnauthorizedException>(() => ctx.Service.RefreshAsync(third.RefreshToken));
    }

    [Fact]
    public async Task Refresh_ReuseInOneFamily_DoesNotAffectOtherFamilies()
    {
        var ctx = new Context();
        var session1 = await ctx.RegisterAsync();
        var session2 = await ctx.Service.LoginAsync(new LoginRequest("a@b.com", StrongPassword));
        var rotated = await ctx.Service.RefreshAsync(session1.RefreshToken);

        await Assert.ThrowsAsync<UnauthorizedException>(() => ctx.Service.RefreshAsync(session1.RefreshToken));

        Assert.True(ctx.Stored(rotated.RefreshToken).Revoked);
        Assert.False(ctx.Stored(session2.RefreshToken).Revoked);
        await ctx.Service.RefreshAsync(session2.RefreshToken);
    }

    [Fact]
    public async Task Refresh_ExpiredToken_ThrowsUnauthorized_AndCreatesNothing()
    {
        var ctx = new Context();
        var first = await ctx.RegisterAsync();
        ctx.Time.Advance(TimeSpan.FromDays(7));

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => ctx.Service.RefreshAsync(first.RefreshToken));

        Assert.Equal("Sessao invalida ou expirada.", ex.Message);
        Assert.Single(ctx.RefreshTokens.Items);
        Assert.False(ctx.Stored(first.RefreshToken).Revoked);
    }

    [Theory]
    [InlineData("nao-existe")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Refresh_UnknownOrEmptyToken_ThrowsUnauthorized_WithGenericMessage(string? token)
    {
        var ctx = new Context();
        await ctx.RegisterAsync();

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => ctx.Service.RefreshAsync(token!));

        Assert.Equal("Sessao invalida ou expirada.", ex.Message);
        Assert.Single(ctx.RefreshTokens.Items);
    }

    [Fact]
    public async Task Refresh_LosingTheRevokeRace_RevokesFamily_AndThrowsUnauthorized()
    {
        var ctx = new Context();
        var first = await ctx.RegisterAsync();
        ctx.RefreshTokens.ForceTryRevokeFailure = true;

        await Assert.ThrowsAsync<UnauthorizedException>(() => ctx.Service.RefreshAsync(first.RefreshToken));

        // O token novo e gravado antes do UPDATE condicional; quem perde a corrida revoga a familia
        // inteira, inclusive esse token novo: nenhum token da cadeia sobrevive.
        Assert.Equal(2, ctx.RefreshTokens.Items.Count);
        Assert.All(ctx.RefreshTokens.Items, token => Assert.True(token.Revoked));
    }

    [Fact]
    public async Task Refresh_UserNoLongerExists_ThrowsUnauthorized_AndRevokesPresentedToken()
    {
        var ctx = new Context();
        var first = await ctx.RegisterAsync();
        ctx.Users.Items.Clear();

        await Assert.ThrowsAsync<UnauthorizedException>(() => ctx.Service.RefreshAsync(first.RefreshToken));

        Assert.True(ctx.Stored(first.RefreshToken).Revoked);
        Assert.Single(ctx.RefreshTokens.Items);
    }

    [Fact]
    public async Task Logout_RevokesToken_AndItCanNoLongerRefresh()
    {
        var ctx = new Context();
        var first = await ctx.RegisterAsync();

        await ctx.Service.LogoutAsync(first.RefreshToken);

        Assert.True(ctx.Stored(first.RefreshToken).Revoked);
        await Assert.ThrowsAsync<UnauthorizedException>(() => ctx.Service.RefreshAsync(first.RefreshToken));
    }

    [Fact]
    public async Task Logout_WithAlreadyRotatedToken_RevokesWholeFamily()
    {
        var ctx = new Context();
        var first = await ctx.RegisterAsync();
        // Atacante rotacionou o token roubado; a vitima ainda tem o antigo e faz logout.
        var attacker = await ctx.Service.RefreshAsync(first.RefreshToken);

        await ctx.Service.LogoutAsync(first.RefreshToken);

        Assert.True(ctx.Stored(attacker.RefreshToken).Revoked);
        await Assert.ThrowsAsync<UnauthorizedException>(() => ctx.Service.RefreshAsync(attacker.RefreshToken));
    }

    [Theory]
    [InlineData("nao-existe")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Logout_UnknownOrEmptyToken_DoesNotThrow(string? token)
    {
        var ctx = new Context();
        await ctx.RegisterAsync();

        await ctx.Service.LogoutAsync(token!);

        Assert.False(ctx.RefreshTokens.Items.Single().Revoked);
    }

    [Fact]
    public async Task Logout_Twice_IsIdempotent()
    {
        var ctx = new Context();
        var first = await ctx.RegisterAsync();

        await ctx.Service.LogoutAsync(first.RefreshToken);
        var revokedAt = ctx.Stored(first.RefreshToken).RevokedAt;
        ctx.Time.Advance(TimeSpan.FromMinutes(5));
        await ctx.Service.LogoutAsync(first.RefreshToken);

        Assert.Equal(revokedAt, ctx.Stored(first.RefreshToken).RevokedAt);
    }
}
