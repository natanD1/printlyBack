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
        public AuthService Service { get; }

        public Context()
        {
            Service = new AuthService(Users, Invites, new FakePasswordHasher(), new FakeTokenService(), new FakeUnitOfWork());
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

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Items { get; } = new();

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(u => u.Id == id));

        public Task<List<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(u => ids.Contains(u.Id)).ToList());

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(u => u.Email == email.Trim().ToLowerInvariant()));

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(u => u.Email == email.Trim().ToLowerInvariant()));

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            Items.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInviteCodeRepository : IInviteCodeRepository
    {
        public List<InviteCode> Items { get; } = new();

        public Task<InviteCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task<InviteCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Code == code.Trim().ToUpperInvariant()));

        public Task<List<InviteCode>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.ToList());

        public Task<List<InviteCode>> GetExpiredUnusedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<InviteCode>());

        public Task AddAsync(InviteCode inviteCode, CancellationToken cancellationToken = default)
        {
            Items.Add(inviteCode);
            return Task.CompletedTask;
        }

        public void Remove(InviteCode inviteCode) => Items.Remove(inviteCode);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "hash:" + password;
        public bool Verify(string password, string hash) => hash == "hash:" + password;
    }

    private sealed class FakeTokenService : ITokenService
    {
        public (string Token, DateTime ExpiresAt) GenerateToken(User user) => ("token", DateTime.UtcNow.AddHours(1));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
