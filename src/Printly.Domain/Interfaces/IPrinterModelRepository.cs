using Printly.Domain.Entities;

namespace Printly.Domain.Interfaces;

public interface IPrinterModelRepository
{
    Task<PrinterModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<PrinterModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(PrinterModel entity, CancellationToken cancellationToken = default);
}
