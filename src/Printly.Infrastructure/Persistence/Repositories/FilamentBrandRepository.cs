using Microsoft.EntityFrameworkCore;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Infrastructure.Persistence.Repositories;

public class FilamentBrandRepository : IFilamentBrandRepository
{
    private readonly PrintlyDbContext _context;

    public FilamentBrandRepository(PrintlyDbContext context)
    {
        _context = context;
    }

    public Task<FilamentBrand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.FilamentsBrands.FirstOrDefaultAsync(fb => fb.Id == id, cancellationToken);

    public Task<List<FilamentBrand>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.FilamentsBrands
            .OrderBy(fb => fb.NameBrand)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(FilamentBrand filamentBrand, CancellationToken cancellationToken = default) =>
        await _context.FilamentsBrands.AddAsync(filamentBrand, cancellationToken);
}
