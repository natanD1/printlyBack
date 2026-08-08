using PrintFlow.Domain.Entities;

namespace PrintFlow.Domain.Interfaces;

public interface IInviteCodeRepository
{
    Task<InviteCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InviteCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<List<InviteCode>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<InviteCode>> GetExpiredUnusedAsync(CancellationToken cancellationToken = default);
    Task AddAsync(InviteCode inviteCode, CancellationToken cancellationToken = default);
    void Remove(InviteCode inviteCode);
}
