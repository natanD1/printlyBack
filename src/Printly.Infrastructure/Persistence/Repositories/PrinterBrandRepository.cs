using Microsoft.EntityFrameworkCore;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Infrastructure.Persistence.Repositories;

public class PrinterBrandRepository : IPrinterBrandRepository
{
    private readonly PrintlyDbContext _context;

    public PrinterBrandRepository(PrintlyDbContext context)
    {
        _context = context;
    }

    public Task<PrinterBrand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PrinterBrands.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<List<PrinterBrand>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.PrinterBrands
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PrinterBrand entity, CancellationToken cancellationToken = default) =>
        await _context.PrinterBrands.AddAsync(entity, cancellationToken);
}
