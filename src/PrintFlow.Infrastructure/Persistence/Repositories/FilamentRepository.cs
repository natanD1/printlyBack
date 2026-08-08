using Microsoft.EntityFrameworkCore;
using PrintFlow.Domain.Entities;
using PrintFlow.Domain.Interfaces;

namespace PrintFlow.Infrastructure.Persistence.Repositories;

public class FilamentRepository : IFilamentRepository
{
    private readonly PrintFlowDbContext _context;

    public FilamentRepository(PrintFlowDbContext context)
    {
        _context = context;
    }

    public Task<Filament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Filaments.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public Task<List<Filament>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.Filaments
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<List<Filament>> GetByIdsAsync(List<Guid> ids, Guid userId, CancellationToken cancellationToken = default) =>
        _context.Filaments
            .Where(f => ids.Contains(f.Id) && f.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Filament filament, CancellationToken cancellationToken = default) =>
        await _context.Filaments.AddAsync(filament, cancellationToken);

    public void Remove(Filament filament) => _context.Filaments.Remove(filament);
}
