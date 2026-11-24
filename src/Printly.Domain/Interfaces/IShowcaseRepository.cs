using Printly.Domain.Entities;

namespace Printly.Domain.Interfaces;

/// <summary>
/// Dados de produto que a vitrine precisa, sem carregar a foto (longtext). Sempre filtrados pelo dono.
/// </summary>
public sealed record ShowcaseProductSummary(Guid Id, string Name, decimal UnitSalePrice, bool HasPhoto, bool IsProduced);

public interface IShowcaseRepository
{
    /// <summary>Vitrine com itens.</summary>
    Task<Showcase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Vitrine com itens.</summary>
    Task<Showcase?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Vitrine com itens.</summary>
    Task<Showcase?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Todas as vitrines com itens, ordenadas por slug (tela admin).</summary>
    Task<List<Showcase>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, Guid? exceptShowcaseId = null, CancellationToken cancellationToken = default);

    /// <summary>Itens visiveis cujo produto esta produzido (uso do limite MaxShowcaseItems).</summary>
    Task<int> CountVisibleProducedItemsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Resumo dos produtos informados que pertencem ao usuario (ids alheios/inexistentes ficam de fora).</summary>
    Task<List<ShowcaseProductSummary>> GetProductSummariesAsync(Guid userId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Produtos produzidos do usuario que nao estao na vitrine dele, ordenados por nome. Sem janela de historico.
    /// <paramref name="search"/> filtra o nome (contem), via LINQ.
    /// </summary>
    Task<List<ShowcaseProductSummary>> GetAvailableProductsAsync(Guid userId, string? search, CancellationToken cancellationToken = default);

    /// <summary>Foto (base64) de um produto do usuario; nulo se nao existir, for de outro usuario ou nao tiver foto.</summary>
    Task<string?> GetProductPhotoAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);

    Task AddAsync(Showcase showcase, CancellationToken cancellationToken = default);
}
