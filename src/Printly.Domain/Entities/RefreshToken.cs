using Printly.Domain.Exceptions;

namespace Printly.Domain.Entities;

/// <summary>
/// Refresh token opaco de uma sessao. Guarda so o hash (SHA-256 hex) do valor entregue ao cliente.
/// Todos os tokens da mesma cadeia de rotacao compartilham o <see cref="FamilyId"/>.
/// </summary>
public class RefreshToken : BaseEntity
{
    public const int TokenHashLength = 64;

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public Guid FamilyId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool Revoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    protected RefreshToken()
    {
    }

    public RefreshToken(Guid userId, string tokenHash, Guid familyId, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length != TokenHashLength)
            throw new DomainException("Hash de refresh token invalido.");

        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        ExpiresAt = expiresAt;
    }

    public bool IsExpired(DateTime now) => now >= ExpiresAt;

    /// <summary>Idempotente: revogar de novo nao altera o instante da primeira revogacao.</summary>
    public void Revoke(DateTime now)
    {
        if (Revoked)
            return;

        Revoked = true;
        RevokedAt = now;
    }
}
