using PrintFlow.Domain.Entities;

namespace PrintFlow.Domain.Interfaces;

public interface IFilamentBrandRepository
{
    Task<FilamentBrand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<FilamentBrand>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(FilamentBrand filamentBrand, CancellationToken cancellationToken = default);
}
