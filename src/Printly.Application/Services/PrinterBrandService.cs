using Printly.Application.DTOs.PrinterBrands;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

public class PrinterBrandService : IPrinterBrandService
{
    private readonly IPrinterBrandRepository _printerBrandRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PrinterBrandService(IPrinterBrandRepository printerBrandRepository, IUnitOfWork unitOfWork)
    {
        _printerBrandRepository = printerBrandRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PrinterBrandDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var printerBrands = await _printerBrandRepository.GetAllAsync(cancellationToken);
        return printerBrands.Select(ToDto).ToList();
    }

    public async Task<PrinterBrandDto> CreateAsync(SavePrinterBrandRequest request, CancellationToken cancellationToken = default)
    {
        var printerBrand = new PrinterBrand(request.Name);

        await _printerBrandRepository.AddAsync(printerBrand, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(printerBrand);
    }

    public async Task<PrinterBrandDto> UpdateAsync(Guid id, SavePrinterBrandRequest request, CancellationToken cancellationToken = default)
    {
        var printerBrand = await _printerBrandRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Marca de impressora nao encontrada.");

        printerBrand.Update(request.Name, request.IsActive);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(printerBrand);
    }

    private static PrinterBrandDto ToDto(PrinterBrand printerBrand) => new(
        printerBrand.Id,
        printerBrand.Name,
        printerBrand.IsActive,
        printerBrand.CreatedAt,
        printerBrand.UpdatedAt);
}
