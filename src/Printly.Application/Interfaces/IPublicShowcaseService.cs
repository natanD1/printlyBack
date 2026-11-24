using Printly.Application.DTOs.Showcases;
using Printly.Application.Services;

namespace Printly.Application.Interfaces;

/// <summary>Pagina publica da vitrine (anonima). Slug invalido ou inexistente lanca NotFoundException.</summary>
public interface IPublicShowcaseService
{
    Task<PublicShowcaseResponse> GetAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Nulo = 404 (indisponivel, sem logo, plano sem CompanyLogoInQuote ou tipo invalido).</summary>
    Task<ImageContent?> GetLogoAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Nulo = 404 (indisponivel, item fora do corte do plano, sem foto ou tipo invalido).</summary>
    Task<ImageContent?> GetItemPhotoAsync(string slug, Guid productId, CancellationToken cancellationToken = default);
}
