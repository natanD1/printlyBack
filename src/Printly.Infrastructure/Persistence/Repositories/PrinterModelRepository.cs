using Microsoft.EntityFrameworkCore;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Infrastructure.Persistence.Repositories;

public class PrinterModelRepository : IPrinterModelRepository
{
    private readonly PrintlyDbContext _context;

    public PrinterModelRepository(PrintlyDbContext context)
    {
        _context = context;
    }

    public Task<PrinterModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PrinterModels.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<List<PrinterModel>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.PrinterModels
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PrinterModel entity, CancellationToken cancellationToken = default) =>
        await _context.PrinterModels.AddAsync(entity, cancellationToken);
}
