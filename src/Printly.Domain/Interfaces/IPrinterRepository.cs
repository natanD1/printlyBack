using Printly.Domain.Entities;

namespace Printly.Domain.Interfaces;

public interface IPrinterRepository
{
    Task<Printer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Printer>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Printer>> GetByIdsAsync(List<Guid> ids, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Printer printer, CancellationToken cancellationToken = default);
    void Remove(Printer printer);
}
