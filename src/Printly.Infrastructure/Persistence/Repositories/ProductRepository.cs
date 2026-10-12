using Microsoft.EntityFrameworkCore;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly PrintlyDbContext _context;

    public ProductRepository(PrintlyDbContext context)
    {
        _context = context;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Products
            .Include(p => p.Filaments)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<List<Product>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.Products
            .Include(p => p.Filaments)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        await _context.Products.AddAsync(product, cancellationToken);

    public void Remove(Product product) => _context.Products.Remove(product);
}
