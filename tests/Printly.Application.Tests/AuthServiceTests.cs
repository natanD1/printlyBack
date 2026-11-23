using Printly.Application.DTOs.Auth;
using Printly.Application.Interfaces;
using Printly.Application.Services;
using Printly.Domain.Entities;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Tests;

public class AuthServiceTests
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

        public InviteCode AddInvite(string code = "ABC123")
        {
            var invite = new InviteCode(code, null);
            Invites.Items.Add(invite);
            return invite;
        }

        public Task<AuthResponse> Register(string email, string password = StrongPassword, string code = "ABC123") =>
            Service.RegisterAsync(new RegisterRequest("Fulano", email, password, code));
    }

    [Fact]
    public async Task Register_And_Login_ReturnBothTokens_AndPersistOnlyTheHash()
    {
        var ctx = new Context();
        ctx.AddInvite();

        var registered = await ctx.Register("a@b.com");
        var logged = await ctx.Service.LoginAsync(new LoginRequest("a@b.com", StrongPassword));

        foreach (var response in new[] { registered, logged })
        {
            Assert.False(string.IsNullOrEmpty(response.AccessToken));
            Assert.False(string.IsNullOrEmpty(response.RefreshToken));
            Assert.Equal(900, response.AccessTokenExpiresIn);
            Assert.Equal(604800, response.RefreshTokenExpiresIn);
            var stored = Assert.Single(ctx.RefreshTokens.Items, t => t.TokenHash == ctx.Tokens.HashRefreshToken(response.RefreshToken));
            Assert.Equal(response.User.Id, stored.UserId);
            Assert.Equal(ctx.Time.UtcNow.UtcDateTime.AddDays(7), stored.ExpiresAt);
        }

        Assert.Equal(2, ctx.RefreshTokens.Items.Count);
        Assert.DoesNotContain(ctx.RefreshTokens.Items, t => t.TokenHash == registered.RefreshToken || t.TokenHash == logged.RefreshToken);
        Assert.All(ctx.RefreshTokens.Items, t => Assert.Equal(64, t.TokenHash.Length));
        // Cada login abre uma familia nova.
        Assert.Equal(2, ctx.RefreshTokens.Items.Select(t => t.FamilyId).Distinct().Count());
    }

    [Fact]
    public async Task Register_NewUser_StartsWithoutOnboarding()
    {
        var ctx = new Context();
        ctx.AddInvite();

        var response = await ctx.Register("a@b.com");

        Assert.False(response.User.OnboardingCompleted);
        Assert.Null(ctx.Users.Items.Single().OnboardingCompletedAt);
    }

    [Fact]
    public async Task CompleteOnboarding_MarksUserAsCompleted()
    {
        var ctx = new Context();
        ctx.AddInvite();
        var response = await ctx.Register("a@b.com");

        var dto = await ctx.Service.CompleteOnboardingAsync(response.User.Id);

        Assert.True(dto.OnboardingCompleted);
        Assert.NotNull(ctx.Users.Items.Single().OnboardingCompletedAt);
    }

    [Fact]
    public async Task CompleteOnboarding_Twice_KeepsFirstTimestamp()
    {
        var ctx = new Context();
        ctx.AddInvite();
        var response = await ctx.Register("a@b.com");

        await ctx.Service.CompleteOnboardingAsync(response.User.Id);
        var user = ctx.Users.Items.Single();
        var first = user.OnboardingCompletedAt;
        var updatedAt = user.UpdatedAt;
        await Task.Delay(20);

        var dto = await ctx.Service.CompleteOnboardingAsync(response.User.Id);

        Assert.True(dto.OnboardingCompleted);
        Assert.Equal(first, user.OnboardingCompletedAt);
        Assert.Equal(updatedAt, user.UpdatedAt);
    }

    [Fact]
    public async Task CompleteOnboarding_UnknownUser_Throws()
    {
        var ctx = new Context();

        var ex = await Assert.ThrowsAsync<DomainException>(() => ctx.Service.CompleteOnboardingAsync(Guid.NewGuid()));
        Assert.Equal("Usuario nao encontrado.", ex.Message);
    }

    [Fact]
    public async Task LoginAndGetCurrentUser_ReflectOnboardingState()
    {
        var ctx = new Context();
        ctx.AddInvite();
        var response = await ctx.Register("a@b.com");

        var loginBefore = await ctx.Service.LoginAsync(new LoginRequest("a@b.com", StrongPassword));
        var meBefore = await ctx.Service.GetCurrentUserAsync(response.User.Id);
        Assert.False(loginBefore.User.OnboardingCompleted);
        Assert.False(meBefore.OnboardingCompleted);

        await ctx.Service.CompleteOnboardingAsync(response.User.Id);

        var loginAfter = await ctx.Service.LoginAsync(new LoginRequest("a@b.com", StrongPassword));
        var meAfter = await ctx.Service.GetCurrentUserAsync(response.User.Id);
        Assert.True(loginAfter.User.OnboardingCompleted);
        Assert.True(meAfter.OnboardingCompleted);
    }

    [Fact]
    public async Task CompleteOnboarding_ForUserA_DoesNotChangeUserB()
    {
        var ctx = new Context();
        ctx.AddInvite("AAA111");
        ctx.AddInvite("BBB222");
        var a = await ctx.Register("a@b.com", code: "AAA111");
        var b = await ctx.Register("b@b.com", code: "BBB222");

        await ctx.Service.CompleteOnboardingAsync(a.User.Id);

        Assert.True((await ctx.Service.GetCurrentUserAsync(a.User.Id)).OnboardingCompleted);
        Assert.False((await ctx.Service.GetCurrentUserAsync(b.User.Id)).OnboardingCompleted);
    }

    [Fact]
    public async Task Register_WeakPassword_Throws_WithoutCreatingUserOrConsumingInvite()
    {
        var ctx = new Context();
        var invite = ctx.AddInvite();

        var ex = await Assert.ThrowsAsync<DomainException>(() => ctx.Register("a@b.com", password: "abc"));

        Assert.StartsWith("A senha deve ter", ex.Message);
        Assert.Empty(ctx.Users.Items);
        Assert.Equal(Domain.Enums.InviteCodeStatus.Available, invite.Status);
        Assert.Null(invite.UsedByUserId);
    }

    [Fact]
    public async Task Register_WeakPassword_FailsBeforeInviteLookup()
    {
        var ctx = new Context();

        var ex = await Assert.ThrowsAsync<DomainException>(() => ctx.Register("a@b.com", password: "abc", code: "NOPE"));

        Assert.StartsWith("A senha deve ter", ex.Message);
    }

    [Fact]
    public async Task Login_ExistingUserWithWeakPassword_StillWorks()
    {
        var ctx = new Context();
        ctx.Users.Items.Add(new User("Antigo", "old@b.com", "hash:123", Guid.NewGuid()));

        var response = await ctx.Service.LoginAsync(new LoginRequest("old@b.com", "123"));

        Assert.Equal("old@b.com", response.User.Email);
    }
}
