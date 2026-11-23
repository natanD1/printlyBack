using Printly.Domain.Entities;

namespace Printly.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Produtos do usuario, opcionalmente filtrados por CreatedAt (UTC): from inclusivo, to exclusivo.
    /// </summary>
    Task<List<Product>> GetAllByUserIdAsync(Guid userId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    /// <summary>Produtos criados pelo usuario no intervalo [from, to) de CreatedAt (UTC), orcamento ou produzido.</summary>
    Task<int> CountCreatedByUserIdAsync(Guid userId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    void Remove(Product product);
}
