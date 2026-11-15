using Printly.Application.DTOs.PrinterModels;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

public class PrinterModelService : IPrinterModelService
{
    private readonly IPrinterModelRepository _printerModelRepository;
    private readonly IPrinterBrandRepository _printerBrandRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PrinterModelService(
        IPrinterModelRepository printerModelRepository,
        IPrinterBrandRepository printerBrandRepository,
        IUnitOfWork unitOfWork)
    {
        _printerModelRepository = printerModelRepository;
        _printerBrandRepository = printerBrandRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PrinterModelDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var printerModels = await _printerModelRepository.GetAllAsync(cancellationToken);
        var brandNames = (await _printerBrandRepository.GetAllAsync(cancellationToken)).ToDictionary(b => b.Id, b => b.Name);

        return printerModels
            .Select(m => ToDto(m, brandNames.GetValueOrDefault(m.BrandId, string.Empty)))
            .OrderBy(m => m.BrandName)
            .ThenBy(m => m.Name)
            .ToList();
    }

    public async Task<PrinterModelDto> CreateAsync(SavePrinterModelRequest request, CancellationToken cancellationToken = default)
    {
        var brand = await GetBrandAsync(request.BrandId, cancellationToken);
        var printerModel = new PrinterModel(request.Name, brand.Id, request.ImageBase64);

        await _printerModelRepository.AddAsync(printerModel, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(printerModel, brand.Name);
    }

    public async Task<PrinterModelDto> UpdateAsync(Guid id, SavePrinterModelRequest request, CancellationToken cancellationToken = default)
    {
        var printerModel = await _printerModelRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Modelo de impressora nao encontrado.");
        var brand = await GetBrandAsync(request.BrandId, cancellationToken);

        printerModel.Update(request.Name, brand.Id, request.ImageBase64, request.IsActive);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(printerModel, brand.Name);
    }

    private async Task<PrinterBrand> GetBrandAsync(Guid brandId, CancellationToken cancellationToken) =>
        await _printerBrandRepository.GetByIdAsync(brandId, cancellationToken)
            ?? throw new DomainException("Marca de impressora nao encontrada.");

    private static PrinterModelDto ToDto(PrinterModel printerModel, string brandName) => new(
        printerModel.Id,
        printerModel.Name,
        printerModel.BrandId,
        brandName,
        printerModel.Image,
        printerModel.IsActive,
        printerModel.CreatedAt,
        printerModel.UpdatedAt);
}
