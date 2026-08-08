using PrintFlow.Domain.Entities;

namespace PrintFlow.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
