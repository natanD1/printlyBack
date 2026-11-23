using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Application.Tests;

internal sealed class FakeTimeProvider : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => UtcNow;

    public void Advance(TimeSpan span) => UtcNow += span;
}

internal sealed class FakeUserRepository : IUserRepository
{
    public List<User> Items { get; } = new();

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(u => u.Id == id));

    public Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.OrderBy(u => u.Name).ToList());

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

internal sealed class FakeInviteCodeRepository : IInviteCodeRepository
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

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hash:" + password;
    public bool Verify(string password, string hash) => hash == "hash:" + password;
}

internal sealed class FakeTokenService : ITokenService
{
    private int _counter;

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(7);

    public (string Token, int ExpiresInSeconds) GenerateAccessToken(User user) => ("access-" + user.Id, 900);

    public string GenerateRefreshToken() => "refresh-" + ++_counter;

    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(refreshToken))).ToLowerInvariant();
}

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Items { get; } = new();

    /// <summary>Simula a corrida: outra requisicao revogou entre a leitura e o UPDATE condicional.</summary>
    public bool ForceTryRevokeFailure { get; set; }

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        Items.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(t => t.TokenHash == tokenHash));

    public Task<bool> TryRevokeAsync(Guid id, DateTime now, CancellationToken cancellationToken = default)
    {
        var token = Items.FirstOrDefault(t => t.Id == id);

        if (ForceTryRevokeFailure || token is null || token.Revoked)
            return Task.FromResult(false);

        token.Revoke(now);
        return Task.FromResult(true);
    }

    public Task RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken cancellationToken = default)
    {
        foreach (var token in Items.Where(t => t.FamilyId == familyId))
            token.Revoke(now);

        return Task.CompletedTask;
    }

    public Task<int> DeleteExpiredAsync(DateTime now, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.RemoveAll(t => t.ExpiresAt < now));
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}
