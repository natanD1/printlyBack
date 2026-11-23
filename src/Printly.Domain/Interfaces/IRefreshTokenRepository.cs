using Printly.Domain.Entities;

namespace Printly.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoga de forma atomica (UPDATE condicional, ja gravado no banco, fora do UnitOfWork).
    /// Devolve true somente se este chamador foi quem revogou (exatamente 1 linha afetada).
    /// </summary>
    Task<bool> TryRevokeAsync(Guid id, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>Revoga todos os tokens ainda ativos da familia (gravado imediatamente, fora do UnitOfWork).</summary>
    Task RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>Apaga tokens com <c>ExpiresAt</c> menor que <paramref name="now"/>. Devolve quantos foram apagados.</summary>
    Task<int> DeleteExpiredAsync(DateTime now, CancellationToken cancellationToken = default);
}
