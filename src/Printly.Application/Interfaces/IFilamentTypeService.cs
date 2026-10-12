using Printly.Application.DTOs.FilamentTypes;

namespace Printly.Application.Interfaces;

public interface IFilamentTypeService
{
    Task<List<FilamentTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FilamentTypeDto> CreateAsync(CreateFilamentTypeRequest request, CancellationToken cancellationToken = default);
    Task<FilamentTypeDto> UpdateAsync(Guid id, UpdateFilamentTypeRequest request, CancellationToken cancellationToken = default);
}
