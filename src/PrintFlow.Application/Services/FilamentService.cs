using PrintFlow.Application.DTOs.Filaments;
using PrintFlow.Application.Interfaces;
using PrintFlow.Domain.Entities;
using PrintFlow.Domain.Exceptions;
using PrintFlow.Domain.Interfaces;

namespace PrintFlow.Application.Services;

public class FilamentService : IFilamentService
{
    private readonly IFilamentRepository _filamentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public FilamentService(IFilamentRepository filamentRepository, IUnitOfWork unitOfWork)
    {
        _filamentRepository = filamentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<FilamentDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var filaments = await _filamentRepository.GetAllByUserIdAsync(userId, cancellationToken);
        return filaments.Select(ToDto).ToList();
    }

    public async Task<FilamentDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var filament = await GetOwnedFilamentAsync(id, userId, cancellationToken);
        return ToDto(filament);
    }

    public async Task<FilamentDto> CreateAsync(CreateFilamentRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var filament = new Filament(request.Name, request.Brand, request.ColorHex, request.Weight, request.FilamentPrice, userId);

        await _filamentRepository.AddAsync(filament, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(filament);
    }

    public async Task<FilamentDto> UpdateAsync(Guid id, UpdateFilamentRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var filament = await GetOwnedFilamentAsync(id, userId, cancellationToken);

        filament.Update(request.Name, request.Brand, request.ColorHex, request.Weight, request.FilamentPrice);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(filament);
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

    private static FilamentDto ToDto(Filament filament) => new(
        filament.Id,
        filament.Name,
        filament.Brand,
        filament.ColorHex,
        filament.Weight,
        filament.FilamentPrice,
        filament.CreatedAt,
        filament.UpdatedAt);
}
