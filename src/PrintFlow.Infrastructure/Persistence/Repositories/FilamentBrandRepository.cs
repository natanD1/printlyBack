using Microsoft.EntityFrameworkCore;
using PrintFlow.Domain.Entities;
using PrintFlow.Domain.Interfaces;

namespace PrintFlow.Infrastructure.Persistence.Repositories;

public class FilamentBrandRepository : IFilamentBrandRepository
{
    private readonly PrintFlowDbContext _context;

    public FilamentBrandRepository(PrintFlowDbContext context)
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
