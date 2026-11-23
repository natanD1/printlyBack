using Printly.Application.DTOs.Printers;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Enums;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

public class PrinterService : IPrinterService
{
    private readonly IPrinterRepository _printerRepository;
    private readonly IPrinterModelRepository _printerModelRepository;
    private readonly IPrinterBrandRepository _printerBrandRepository;
    private readonly IEntitlementService _entitlementService;
    private readonly IUnitOfWork _unitOfWork;

    public PrinterService(
        IPrinterRepository printerRepository,
        IPrinterModelRepository printerModelRepository,
        IPrinterBrandRepository printerBrandRepository,
        IEntitlementService entitlementService,
        IUnitOfWork unitOfWork)
    {
        _printerRepository = printerRepository;
        _printerModelRepository = printerModelRepository;
        _printerBrandRepository = printerBrandRepository;
        _entitlementService = entitlementService;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PrinterDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var printers = await _printerRepository.GetAllByUserIdAsync(userId, cancellationToken);
        var catalog = await PrinterCatalog.LoadAsync(_printerModelRepository, _printerBrandRepository, cancellationToken);

        return printers.Select(p => ToDto(p, catalog)).ToList();
    }

    public async Task<PrinterDto> CreateAsync(SavePrinterRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        await _entitlementService.EnsureWithinLimitAsync(userId, PlanEntitlementKey.MaxPrinters, cancellationToken);
        await EnsureOptionalCostsAllowedAsync(request, false, false, false, userId, cancellationToken);
        await EnsureActiveModelAsync(request.PrinterModelId, cancellationToken);

        var printer = new Printer(
            request.PrinterModelId,
            request.Nickname,
            request.AveragePowerWatts,
            request.DefaultProfitMarginPercentage,
            request.PrinterValue,
            request.PrinterLifespanHours,
            request.MaintenancePercentage,
            request.FailureRatePercentage,
            request.DepreciationEnabled,
            request.MaintenanceEnabled,
            request.FailureEnabled,
            userId);

        await _printerRepository.AddAsync(printer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(printer, cancellationToken);
    }

    public async Task<PrinterDto> UpdateAsync(Guid id, SavePrinterRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var printer = await GetOwnedPrinterAsync(id, userId, cancellationToken);

        await EnsureOptionalCostsAllowedAsync(
            request, printer.DepreciationEnabled, printer.MaintenanceEnabled, printer.FailureEnabled, userId, cancellationToken);

        // Modelo inativo so bloqueia a troca; quem ja tem a impressora continua editando os custos.
        if (request.PrinterModelId != printer.PrinterModelId)
            await EnsureActiveModelAsync(request.PrinterModelId, cancellationToken);

        printer.Update(
            request.PrinterModelId,
            request.Nickname,
            request.AveragePowerWatts,
            request.DefaultProfitMarginPercentage,
            request.PrinterValue,
            request.PrinterLifespanHours,
            request.MaintenancePercentage,
            request.FailureRatePercentage,
            request.DepreciationEnabled,
            request.MaintenanceEnabled,
            request.FailureEnabled);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(printer, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var printer = await GetOwnedPrinterAsync(id, userId, cancellationToken);

        // Produtos que usavam a impressora ficam com PrinterId nulo (FK SetNull) e mantem o preco salvo.
        _printerRepository.Remove(printer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Custos opcionais (depreciacao, manutencao, falha) exigem o recurso do plano so para LIGAR o que esta desligado.
    /// Manter ligado o que ja esta (ex.: plano rebaixado) ou editar outros campos continua permitido.
    /// </summary>
    private async Task EnsureOptionalCostsAllowedAsync(
        SavePrinterRequest request,
        bool depreciationEnabled,
        bool maintenanceEnabled,
        bool failureEnabled,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var turningOn = (request.DepreciationEnabled && !depreciationEnabled)
            || (request.MaintenanceEnabled && !maintenanceEnabled)
            || (request.FailureEnabled && !failureEnabled);

        if (turningOn)
            await _entitlementService.EnsureFeatureAsync(userId, PlanEntitlementKey.OptionalCostsEnabled, cancellationToken);
    }

    private async Task EnsureActiveModelAsync(Guid printerModelId, CancellationToken cancellationToken)
    {
        var printerModel = await _printerModelRepository.GetByIdAsync(printerModelId, cancellationToken);

        if (printerModel is null || !printerModel.IsActive)
            throw new DomainException("Modelo de impressora nao encontrado.");
    }

    private async Task<Printer> GetOwnedPrinterAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var printer = await _printerRepository.GetByIdAsync(id, cancellationToken);

        if (printer is null || printer.UserId != userId)
            throw new DomainException("Impressora nao encontrada.");

        return printer;
    }

    private async Task<PrinterDto> ToDtoAsync(Printer printer, CancellationToken cancellationToken)
    {
        var catalog = await PrinterCatalog.LoadAsync(_printerModelRepository, _printerBrandRepository, cancellationToken);
        return ToDto(printer, catalog);
    }

    private static PrinterDto ToDto(Printer printer, PrinterCatalog catalog)
    {
        var model = catalog.Models.GetValueOrDefault(printer.PrinterModelId);
        var brandName = model is null ? string.Empty : catalog.BrandNames.GetValueOrDefault(model.BrandId, string.Empty);

        return new PrinterDto(
            printer.Id,
            printer.Nickname,
            catalog.DisplayName(printer),
            printer.PrinterModelId,
            model?.Name ?? string.Empty,
            model?.BrandId ?? Guid.Empty,
            brandName,
            model?.Image,
            printer.AveragePowerWatts,
            printer.DefaultProfitMarginPercentage,
            printer.PrinterValue,
            printer.PrinterLifespanHours,
            printer.MaintenancePercentage,
            printer.FailureRatePercentage,
            printer.DepreciationEnabled,
            printer.MaintenanceEnabled,
            printer.FailureEnabled,
            printer.CreatedAt,
            printer.UpdatedAt);
    }
}
