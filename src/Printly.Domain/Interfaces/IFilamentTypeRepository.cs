using Printly.Domain.Entities;

namespace Printly.Domain.Interfaces;

public interface IFilamentTypeRepository
{
    Task<FilamentType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<FilamentType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(FilamentType filamentType, CancellationToken cancellationToken = default);
}
