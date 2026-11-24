using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

/// <summary>
/// Corte pelo limite MaxShowcaseItems, unico para o GET do dono (<c>outsidePlan</c>), a pagina publica e a foto publica.
/// Elegivel = visivel e produto produzido; ordena por SortOrder (desempate CreatedAt) e os <c>limit</c> primeiros
/// ficam "dentro". Limite nulo = todos dentro. Downgrade nunca apaga: so esconde o excedente.
/// </summary>
public static class ShowcasePlanCut
{
    /// <summary>Itens que aparecem no publico, ja na ordem de exibicao.</summary>
    public static List<ShowcaseItem> Inside(
        IEnumerable<ShowcaseItem> items,
        IReadOnlyDictionary<Guid, ShowcaseProductSummary> products,
        int? limit)
    {
        var eligible = items
            .Where(i => IsEligible(i, products))
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.CreatedAt);

        return (limit is { } max ? eligible.Take(Math.Max(0, max)) : eligible).ToList();
    }

    /// <summary>Visivel e com produto (do dono) produzido.</summary>
    public static bool IsEligible(ShowcaseItem item, IReadOnlyDictionary<Guid, ShowcaseProductSummary> products) =>
        item.IsVisible && products.TryGetValue(item.ProductId, out var product) && product.IsProduced;
}
