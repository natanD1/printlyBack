using Microsoft.EntityFrameworkCore;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Infrastructure.Persistence.Repositories;

public class PrinterRepository : IPrinterRepository
{
    private readonly PrintlyDbContext _context;

    public PrinterRepository(PrintlyDbContext context)
    {
        _context = context;
    }

    public Task<Printer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Printers.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<List<Printer>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.Printers
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.Printers.CountAsync(p => p.UserId == userId, cancellationToken);

    public Task<List<Printer>> GetByIdsAsync(List<Guid> ids, Guid userId, CancellationToken cancellationToken = default) =>
        _context.Printers
            .Where(p => ids.Contains(p.Id) && p.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Printer printer, CancellationToken cancellationToken = default) =>
        await _context.Printers.AddAsync(printer, cancellationToken);

    public void Remove(Printer printer) => _context.Printers.Remove(printer);
}
