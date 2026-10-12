using Printly.Application.DTOs.FilamentBrands;
using Printly.Application.DTOs.Filaments;
using Printly.Application.DTOs.FilamentTypes;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

public class FilamentService : IFilamentService
{
    private readonly IFilamentRepository _filamentRepository;
    private readonly IFilamentBrandRepository _filamentBrandRepository;
    private readonly IFilamentTypeRepository _filamentTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public FilamentService(
        IFilamentRepository filamentRepository,
        IFilamentBrandRepository filamentBrandRepository,
        IFilamentTypeRepository filamentTypeRepository,
        IUnitOfWork unitOfWork)
    {
        _filamentRepository = filamentRepository;
        _filamentBrandRepository = filamentBrandRepository;
        _filamentTypeRepository = filamentTypeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<FilamentDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var filaments = await _filamentRepository.GetAllByUserIdAsync(userId, cancellationToken);
        var brands = (await _filamentBrandRepository.GetAllAsync(cancellationToken)).ToDictionary(b => b.Id);
        var types = (await _filamentTypeRepository.GetAllAsync(cancellationToken)).ToDictionary(t => t.Id);

        return filaments.Select(f => ToDto(f, brands[f.BrandId], types[f.TypeId])).ToList();
    }

    public async Task<FilamentDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var filament = await GetOwnedFilamentAsync(id, userId, cancellationToken);
        var brand = await GetExistingBrandAsync(filament.BrandId, cancellationToken);
        var type = await GetExistingTypeAsync(filament.TypeId, cancellationToken);

        return ToDto(filament, brand, type);
    }

    public async Task<FilamentDto> CreateAsync(CreateFilamentRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var brand = await GetExistingBrandAsync(request.BrandId, cancellationToken);
        var type = await GetExistingTypeAsync(request.TypeId, cancellationToken);

        var filament = new Filament(request.Name, request.BrandId, request.TypeId, request.ColorHex, request.Weight, request.FilamentPrice, userId);

        await _filamentRepository.AddAsync(filament, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(filament, brand, type);
    }

    public async Task<FilamentDto> UpdateAsync(Guid id, UpdateFilamentRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var filament = await GetOwnedFilamentAsync(id, userId, cancellationToken);
        var brand = await GetExistingBrandAsync(request.BrandId, cancellationToken);
        var type = await GetExistingTypeAsync(request.TypeId, cancellationToken);

        filament.Update(request.Name, request.BrandId, request.TypeId, request.ColorHex, request.Weight, request.FilamentPrice);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(filament, brand, type);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var filament = await GetOwnedFilamentAsync(id, userId, cancellationToken);

        _filamentRepository.Remove(filament);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Filament> GetOwnedFilamentAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var filament = await _filamentRepository.GetByIdAsync(id, cancellationToken);

        if (filament is null || filament.UserId != userId)
            throw new DomainException("Filamento nao encontrado.");

        return filament;
    }

    private async Task<FilamentBrand> GetExistingBrandAsync(Guid brandId, CancellationToken cancellationToken)
    {
        var brand = await _filamentBrandRepository.GetByIdAsync(brandId, cancellationToken);

        if (brand is null)
            throw new DomainException("Marca de filamento nao encontrada.");

        return brand;
    }

    private async Task<FilamentType> GetExistingTypeAsync(Guid typeId, CancellationToken cancellationToken)
    {
        var type = await _filamentTypeRepository.GetByIdAsync(typeId, cancellationToken);

        if (type is null)
            throw new DomainException("Tipo de filamento nao encontrado.");

        return type;
    }

    private static FilamentBrandDto ToBrandDto(FilamentBrand brand) => new(
        brand.Id, brand.NameBrand, brand.ImageBrand, brand.IsActive, brand.CreatedAt, brand.UpdatedAt);

    private static FilamentTypeDto ToTypeDto(FilamentType type) => new(
        type.Id, type.Name, type.IsActive, type.CreatedAt, type.UpdatedAt);

    private static FilamentDto ToDto(Filament filament, FilamentBrand brand, FilamentType type) => new(
        filament.Id,
        filament.Name,
        ToBrandDto(brand),
        ToTypeDto(type),
        filament.ColorHex,
        filament.Weight,
        filament.InitialWeight,
        filament.FilamentPrice,
        filament.CreatedAt,
        filament.UpdatedAt);
}
