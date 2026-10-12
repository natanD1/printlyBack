using Printly.Application.DTOs.Filaments;

namespace Printly.Application.Interfaces;

public interface IFilamentService
{
    Task<List<FilamentDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<FilamentDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<FilamentDto> CreateAsync(CreateFilamentRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<FilamentDto> UpdateAsync(Guid id, UpdateFilamentRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
