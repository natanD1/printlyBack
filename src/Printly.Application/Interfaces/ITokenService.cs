using Printly.Domain.Entities;

namespace Printly.Application.Interfaces;

public interface ITokenService
{
    /// <summary>Tempo de vida do refresh token.</summary>
    TimeSpan RefreshTokenLifetime { get; }

    (string Token, int ExpiresInSeconds) GenerateAccessToken(User user);

    /// <summary>Token opaco: 32 bytes aleatorios em Base64Url sem padding. Nunca persistir o valor cru.</summary>
    string GenerateRefreshToken();

    /// <summary>SHA-256 do token, em hexadecimal minusculo (64 chars).</summary>
    string HashRefreshToken(string refreshToken);
}
