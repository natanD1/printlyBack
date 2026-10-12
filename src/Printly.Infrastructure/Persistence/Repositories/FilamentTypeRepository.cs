using Microsoft.EntityFrameworkCore;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Infrastructure.Persistence.Repositories;

public class FilamentTypeRepository : IFilamentTypeRepository
{
    private readonly PrintlyDbContext _context;

    public FilamentTypeRepository(PrintlyDbContext context)
    {
        _context = context;
    }

    public Task<FilamentType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.FilamentsType.FirstOrDefaultAsync(ft => ft.Id == id, cancellationToken);

    public Task<List<FilamentType>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.FilamentsType
            .OrderBy(ft => ft.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(FilamentType filamentType, CancellationToken cancellationToken = default) =>
        await _context.FilamentsType.AddAsync(filamentType, cancellationToken);
}
