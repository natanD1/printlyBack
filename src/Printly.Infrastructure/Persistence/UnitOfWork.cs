using Printly.Domain.Interfaces;

namespace Printly.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly PrintlyDbContext _context;

    public UnitOfWork(PrintlyDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
