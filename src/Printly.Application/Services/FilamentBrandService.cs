using Printly.Application.DTOs.FilamentBrands;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

public class FilamentBrandService : IFilamentBrandService
{
    private readonly IFilamentBrandRepository _filamentBrandRepository;
    private readonly IUnitOfWork _unitOfWork;

    public FilamentBrandService(IFilamentBrandRepository filamentBrandRepository, IUnitOfWork unitOfWork)
    {
        _filamentBrandRepository = filamentBrandRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<FilamentBrandDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var filamentBrands = await _filamentBrandRepository.GetAllAsync(cancellationToken);
        return filamentBrands.Select(ToDto).ToList();
    }

    public async Task<FilamentBrandDto> CreateAsync(CreateFilamentBrandRequest request, CancellationToken cancellationToken = default)
    {
        var filamentBrand = new FilamentBrand(request.NameBrand, request.ImageBrandBase64);

        await _filamentBrandRepository.AddAsync(filamentBrand, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(filamentBrand);
    }

    public async Task<FilamentBrandDto> UpdateAsync(Guid id, UpdateFilamentBrandRequest request, CancellationToken cancellationToken = default)
    {
        var filamentBrand = await GetExistingAsync(id, cancellationToken);

        filamentBrand.Update(request.NameBrand, request.ImageBrandBase64);

        if (request.IsActive)
            filamentBrand.Activate();
        else
            filamentBrand.Deactivate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(filamentBrand);
    }

    private async Task<FilamentBrand> GetExistingAsync(Guid id, CancellationToken cancellationToken)
    {
        var filamentBrand = await _filamentBrandRepository.GetByIdAsync(id, cancellationToken);

        if (filamentBrand is null)
            throw new DomainException("Marca de filamento nao encontrada.");

        return filamentBrand;
    }

    private static FilamentBrandDto ToDto(FilamentBrand filamentBrand) => new(
        filamentBrand.Id,
        filamentBrand.NameBrand,
        filamentBrand.ImageBrand,
        filamentBrand.IsActive,
        filamentBrand.CreatedAt,
        filamentBrand.UpdatedAt);
}
