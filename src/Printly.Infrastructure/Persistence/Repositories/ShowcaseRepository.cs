using Microsoft.EntityFrameworkCore;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Infrastructure.Persistence.Repositories;

/// <summary>Consultas da vitrine. So LINQ (parametrizado pelo EF); a foto (longtext) so e lida quando pedida.</summary>
public class ShowcaseRepository : IShowcaseRepository
{
    private readonly PrintlyDbContext _context;

    public ShowcaseRepository(PrintlyDbContext context)
    {
        _context = context;
    }

    public Task<Showcase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Showcases.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Showcase?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.Showcases.Include(s => s.Items).FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

    public Task<Showcase?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        _context.Showcases.Include(s => s.Items).FirstOrDefaultAsync(s => s.Slug == slug, cancellationToken);

    public Task<List<Showcase>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Showcases.Include(s => s.Items).OrderBy(s => s.Slug).ToListAsync(cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid? exceptShowcaseId = null, CancellationToken cancellationToken = default) =>
        _context.Showcases.AnyAsync(s => s.Slug == slug && (exceptShowcaseId == null || s.Id != exceptShowcaseId), cancellationToken);

    public Task<int> CountVisibleProducedItemsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        QueryVisibleProducedItems(userId).CountAsync(cancellationToken);

    public async Task<List<ShowcaseProductSummary>> GetProductSummariesAsync(
        Guid userId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
            return new List<ShowcaseProductSummary>();

        var ids = productIds.Distinct().ToList();

        return await QueryProductSummaries(userId, ids).ToListAsync(cancellationToken);
    }

    public Task<List<ShowcaseProductSummary>> GetAvailableProductsAsync(Guid userId, string? search, CancellationToken cancellationToken = default) =>
        QueryAvailableProducts(userId, search).ToListAsync(cancellationToken);

    public Task<string?> GetProductPhotoAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default) =>
        _context.Products
            .Where(p => p.Id == productId && p.UserId == userId)
            .Select(p => p.ProductPhoto)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Showcase showcase, CancellationToken cancellationToken = default) =>
        await _context.Showcases.AddAsync(showcase, cancellationToken);

    /// <summary>Itens visiveis com produto produzido, so do usuario (mesmo dono na vitrine e no produto).</summary>
    public IQueryable<Guid> QueryVisibleProducedItems(Guid userId) =>
        from item in _context.ShowcaseItems
        join showcase in _context.Showcases on item.ShowcaseId equals showcase.Id
        join product in _context.Products on item.ProductId equals product.Id
        where showcase.UserId == userId && product.UserId == userId && item.IsVisible && product.IsProduced
        select item.Id;

    public IQueryable<ShowcaseProductSummary> QueryProductSummaries(Guid userId, List<Guid> ids) =>
        ToSummaries(_context.Products.Where(p => p.UserId == userId && ids.Contains(p.Id)));

    private const string LikeEscape = "\\";

    private static string EscapeLike(string value) =>
        value.Replace(LikeEscape, LikeEscape + LikeEscape)
            .Replace("%", LikeEscape + "%")
            .Replace("_", LikeEscape + "_");

    public IQueryable<ShowcaseProductSummary> QueryAvailableProducts(Guid userId, string? search)
    {
        var inShowcase =
            from item in _context.ShowcaseItems
            join showcase in _context.Showcases on item.ShowcaseId equals showcase.Id
            where showcase.UserId == userId
            select item.ProductId;

        var query = _context.Products.Where(p => p.UserId == userId && p.IsProduced && !inShowcase.Contains(p.Id));

        // So LINQ: o padrao e montado numa variavel local (capturada pela lambda), entao vai como parametro
        // (@__pattern_N) no comando real; % e _ do termo viram literais pelo escape proprio.
        // Obs.: ToQueryString() do Pomelo mostra parametros string inline so na exibicao; o teste confere o
        // DbCommand de verdade (CreateDbCommand).
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = "%" + EscapeLike(search) + "%";
            query = query.Where(p => EF.Functions.Like(p.ProductName, pattern, LikeEscape));
        }

        return ToSummaries(query.OrderBy(p => p.ProductName));
    }

    /// <summary>
    /// Projecao sem a foto: HasPhoto e calculado no banco e o preco unitario usa a conta da entidade
    /// (<see cref="Product.CalculateUnitSalePrice"/>), avaliada no cliente sobre SalePrice/Quantity.
    /// </summary>
    private static IQueryable<ShowcaseProductSummary> ToSummaries(IQueryable<Product> query) =>
        query.Select(p => new ShowcaseProductSummary(
            p.Id,
            p.ProductName,
            Product.CalculateUnitSalePrice(p.SalePrice, p.Quantity),
            p.ProductPhoto != null && p.ProductPhoto != "",
            p.IsProduced));
}
