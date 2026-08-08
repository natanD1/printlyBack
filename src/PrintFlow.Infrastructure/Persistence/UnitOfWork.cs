using PrintFlow.Domain.Interfaces;

namespace PrintFlow.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly PrintFlowDbContext _context;

    public UnitOfWork(PrintFlowDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
