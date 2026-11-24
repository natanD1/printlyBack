using Printly.Application.DTOs.Showcases;
using Printly.Application.Services;

namespace Printly.Application.Interfaces;

/// <summary>
/// Vitrine do dono (escritas exigem ShowcaseEnabled; leitura e remocao de item nao) e acoes do admin.
/// Produto alheio ou inexistente responde igual (404 generico).
/// </summary>
public interface IShowcaseService
{
    Task<ShowcaseStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ShowcaseDto> CreateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ShowcaseDto> SetPublishedAsync(Guid userId, bool isPublished, CancellationToken cancellationToken = default);
    Task<ShowcaseDto> AddItemsAsync(Guid userId, IReadOnlyList<Guid>? productIds, CancellationToken cancellationToken = default);
    Task<ShowcaseDto> ReorderItemsAsync(Guid userId, IReadOnlyList<Guid>? productIds, CancellationToken cancellationToken = default);
    Task<ShowcaseDto> SetItemVisibilityAsync(Guid userId, Guid productId, bool isVisible, CancellationToken cancellationToken = default);
    Task RemoveItemAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<List<AvailableProductDto>> GetAvailableProductsAsync(Guid userId, string? search, CancellationToken cancellationToken = default);

    /// <summary>Foto de qualquer produto do proprio usuario; nulo = 404.</summary>
    Task<ImageContent?> GetProductPhotoAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);

    Task<List<AdminShowcaseDto>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<AdminShowcaseDto> SetBlockedAsync(Guid showcaseId, bool isBlocked, CancellationToken cancellationToken = default);
}
