using Printly.Domain.Entities;

namespace Printly.Domain.Interfaces;

public interface IPrinterBrandRepository
{
    Task<PrinterBrand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<PrinterBrand>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(PrinterBrand entity, CancellationToken cancellationToken = default);
}
