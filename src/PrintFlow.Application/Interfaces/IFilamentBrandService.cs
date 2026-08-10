using PrintFlow.Application.DTOs.FilamentBrands;

namespace PrintFlow.Application.Interfaces;

public interface IFilamentBrandService
{
    Task<List<FilamentBrandDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FilamentBrandDto> CreateAsync(CreateFilamentBrandRequest request, CancellationToken cancellationToken = default);
    Task<FilamentBrandDto> UpdateAsync(Guid id, UpdateFilamentBrandRequest request, CancellationToken cancellationToken = default);
}
