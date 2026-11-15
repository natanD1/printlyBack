using Printly.Application.DTOs.Printers;

namespace Printly.Application.Interfaces;

public interface IPrinterService
{
    Task<List<PrinterDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PrinterDto> CreateAsync(SavePrinterRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<PrinterDto> UpdateAsync(Guid id, SavePrinterRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
