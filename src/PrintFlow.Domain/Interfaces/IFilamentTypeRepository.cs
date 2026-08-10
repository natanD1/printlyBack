using PrintFlow.Domain.Entities;

namespace PrintFlow.Domain.Interfaces;

public interface IFilamentTypeRepository
{
    Task<FilamentType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<FilamentType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(FilamentType filamentType, CancellationToken cancellationToken = default);
}
