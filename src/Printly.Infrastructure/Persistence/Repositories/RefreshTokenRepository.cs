using Microsoft.EntityFrameworkCore;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly PrintlyDbContext _context;

    public RefreshTokenRepository(PrintlyDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _context.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

    // ExecuteUpdate/ExecuteDelete ignoram o change tracker e gravam na hora (fora do UnitOfWork).
    public async Task<bool> TryRevokeAsync(Guid id, DateTime now, CancellationToken cancellationToken = default)
    {
        var affected = await _context.RefreshTokens
            .Where(r => r.Id == id && !r.Revoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Revoked, true)
                .SetProperty(r => r.RevokedAt, (DateTime?)now)
                .SetProperty(r => r.UpdatedAt, (DateTime?)now), cancellationToken);

        return affected == 1;
    }

    public async Task RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken cancellationToken = default) =>
        await _context.RefreshTokens
            .Where(r => r.FamilyId == familyId && !r.Revoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Revoked, true)
                .SetProperty(r => r.RevokedAt, (DateTime?)now)
                .SetProperty(r => r.UpdatedAt, (DateTime?)now), cancellationToken);

    public Task<int> DeleteExpiredAsync(DateTime now, CancellationToken cancellationToken = default) =>
        _context.RefreshTokens
            .Where(r => r.ExpiresAt < now)
            .ExecuteDeleteAsync(cancellationToken);
}
