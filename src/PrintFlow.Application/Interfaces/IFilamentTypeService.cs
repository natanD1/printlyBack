using PrintFlow.Application.DTOs.FilamentTypes;

namespace PrintFlow.Application.Interfaces;

public interface IFilamentTypeService
{
    Task<List<FilamentTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FilamentTypeDto> CreateAsync(CreateFilamentTypeRequest request, CancellationToken cancellationToken = default);
    Task<FilamentTypeDto> UpdateAsync(Guid id, UpdateFilamentTypeRequest request, CancellationToken cancellationToken = default);
}
