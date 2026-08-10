using PrintFlow.Application.DTOs.FilamentTypes;
using PrintFlow.Application.Interfaces;
using PrintFlow.Domain.Entities;
using PrintFlow.Domain.Exceptions;
using PrintFlow.Domain.Interfaces;

namespace PrintFlow.Application.Services;

public class FilamentTypeService : IFilamentTypeService
{
    private readonly IFilamentTypeRepository _filamentTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public FilamentTypeService(IFilamentTypeRepository filamentTypeRepository, IUnitOfWork unitOfWork)
    {
        _filamentTypeRepository = filamentTypeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<FilamentTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var filamentTypes = await _filamentTypeRepository.GetAllAsync(cancellationToken);
        return filamentTypes.Select(ToDto).ToList();
    }

    public async Task<FilamentTypeDto> CreateAsync(CreateFilamentTypeRequest request, CancellationToken cancellationToken = default)
    {
        var filamentType = new FilamentType(request.Name);

        await _filamentTypeRepository.AddAsync(filamentType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(filamentType);
    }

    public async Task<FilamentTypeDto> UpdateAsync(Guid id, UpdateFilamentTypeRequest request, CancellationToken cancellationToken = default)
    {
        var filamentType = await GetExistingAsync(id, cancellationToken);

        filamentType.Update(request.Name);

        if (request.IsActive)
            filamentType.Activate();
        else
            filamentType.Deactivate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(filamentType);
    }

    private async Task<FilamentType> GetExistingAsync(Guid id, CancellationToken cancellationToken)
    {
        var filamentType = await _filamentTypeRepository.GetByIdAsync(id, cancellationToken);

        if (filamentType is null)
            throw new DomainException("Tipo de filamento nao encontrado.");

        return filamentType;
    }

    private static FilamentTypeDto ToDto(FilamentType filamentType) => new(
        filamentType.Id,
        filamentType.Name,
        filamentType.IsActive,
        filamentType.CreatedAt,
        filamentType.UpdatedAt);
}
