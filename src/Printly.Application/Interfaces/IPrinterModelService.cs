using Printly.Application.DTOs.PrinterModels;

namespace Printly.Application.Interfaces;

public interface IPrinterModelService
{
    Task<List<PrinterModelDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PrinterModelDto> CreateAsync(SavePrinterModelRequest request, CancellationToken cancellationToken = default);
    Task<PrinterModelDto> UpdateAsync(Guid id, SavePrinterModelRequest request, CancellationToken cancellationToken = default);
}
