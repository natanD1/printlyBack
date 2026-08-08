using PrintFlow.Domain.Entities;

namespace PrintFlow.Domain.Interfaces;

public interface IFilamentRepository
{
    Task<Filament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Filament>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Filament>> GetByIdsAsync(List<Guid> ids, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Filament filament, CancellationToken cancellationToken = default);
    void Remove(Filament filament);
}
