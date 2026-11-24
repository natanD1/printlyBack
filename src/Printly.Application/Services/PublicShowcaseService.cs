using Printly.Application.DTOs.Showcases;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Enums;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

/// <summary>
/// Leitura anonima da vitrine. Disponivel = publicada, nao bloqueada e dono com ShowcaseEnabled.
/// Itens e fotos seguem o mesmo corte do plano (<see cref="ShowcasePlanCut"/>).
/// </summary>
public class PublicShowcaseService : IPublicShowcaseService
{
    private const string ShowcaseNotFound = "Vitrine nao encontrada.";

    private readonly IShowcaseRepository _showcaseRepository;
    private readonly ICompanyProfileRepository _companyProfileRepository;
    private readonly IEntitlementService _entitlementService;

    public PublicShowcaseService(
        IShowcaseRepository showcaseRepository,
        ICompanyProfileRepository companyProfileRepository,
        IEntitlementService entitlementService)
    {
        _showcaseRepository = showcaseRepository;
        _companyProfileRepository = companyProfileRepository;
        _entitlementService = entitlementService;
    }

    public async Task<PublicShowcaseResponse> GetAsync(string slug, CancellationToken cancellationToken = default)
    {
        var showcase = await FindAsync(slug, cancellationToken) ?? throw new NotFoundException(ShowcaseNotFound);

        if (!await IsAvailableAsync(showcase, cancellationToken))
            return new PublicShowcaseUnavailableDto();

        var ownerId = showcase.UserId;
        var company = await _companyProfileRepository.GetByUserIdAsync(ownerId, cancellationToken);
        var (inside, products) = await GetInsideAsync(showcase, cancellationToken);

        var hasLogo = company?.LogoBase64 is not null
            && await IsFeatureEnabledAsync(ownerId, PlanEntitlementKey.CompanyLogoInQuote, cancellationToken)
            && ImageContent.TryDecode(company.LogoBase64) is not null;

        var showBranding = !await IsFeatureEnabledAsync(ownerId, PlanEntitlementKey.RemoveBrandingFooter, cancellationToken);

        var contacts = new PublicShowcaseContactsDto(
            PublicContactSanitizer.Phone(company?.Phone),
            PublicContactSanitizer.Email(company?.Email),
            PublicContactSanitizer.WhatsApp(company?.WhatsApp),
            PublicContactSanitizer.Instagram(company?.Instagram),
            PublicContactSanitizer.Website(company?.Website));

        var items = inside
            .Select(i => products[i.ProductId])
            .Select(p => new PublicShowcaseItemDto(p.Id, p.Name, p.UnitSalePrice, p.HasPhoto))
            .ToList();

        return new PublicShowcaseDto(company?.Name ?? showcase.Slug, hasLogo, showBranding, contacts, items);
    }

    public async Task<ImageContent?> GetLogoAsync(string slug, CancellationToken cancellationToken = default)
    {
        var showcase = await FindAvailableAsync(slug, cancellationToken);

        if (showcase is null
            || !await IsFeatureEnabledAsync(showcase.UserId, PlanEntitlementKey.CompanyLogoInQuote, cancellationToken))
            return null;

        var company = await _companyProfileRepository.GetByUserIdAsync(showcase.UserId, cancellationToken);
        return ImageContent.TryDecode(company?.LogoBase64);
    }

    public async Task<ImageContent?> GetItemPhotoAsync(string slug, Guid productId, CancellationToken cancellationToken = default)
    {
        var showcase = await FindAvailableAsync(slug, cancellationToken);

        if (showcase is null)
            return null;

        var (inside, _) = await GetInsideAsync(showcase, cancellationToken);

        if (inside.All(i => i.ProductId != productId))
            return null;

        // Busca filtrada pelo dono da vitrine: um slug nunca serve foto de produto de outra conta.
        return ImageContent.TryDecode(await _showcaseRepository.GetProductPhotoAsync(showcase.UserId, productId, cancellationToken));
    }

    /// <summary>Slug fora do padrao nem consulta o banco (a collation do MySQL ignoraria maiusculas).</summary>
    private Task<Showcase?> FindAsync(string slug, CancellationToken cancellationToken) =>
        ShowcaseSlug.IsValid(slug)
            ? _showcaseRepository.GetBySlugAsync(slug, cancellationToken)
            : Task.FromResult<Showcase?>(null);

    private async Task<Showcase?> FindAvailableAsync(string slug, CancellationToken cancellationToken)
    {
        var showcase = await FindAsync(slug, cancellationToken);
        return showcase is not null && await IsAvailableAsync(showcase, cancellationToken) ? showcase : null;
    }

    private async Task<bool> IsAvailableAsync(Showcase showcase, CancellationToken cancellationToken) =>
        showcase.IsPublished
        && !showcase.IsBlocked
        && await IsFeatureEnabledAsync(showcase.UserId, PlanEntitlementKey.ShowcaseEnabled, cancellationToken);

    private Task<bool> IsFeatureEnabledAsync(Guid userId, PlanEntitlementKey key, CancellationToken cancellationToken) =>
        _entitlementService.IsFeatureEnabledAsync(userId, key, cancellationToken);

    private async Task<(List<ShowcaseItem> Inside, Dictionary<Guid, ShowcaseProductSummary> Products)> GetInsideAsync(
        Showcase showcase, CancellationToken cancellationToken)
    {
        var products = (await _showcaseRepository.GetProductSummariesAsync(
                showcase.UserId, showcase.Items.Select(i => i.ProductId).ToList(), cancellationToken))
            .ToDictionary(p => p.Id);

        var limit = await _entitlementService.GetLimitAsync(showcase.UserId, PlanEntitlementKey.MaxShowcaseItems, cancellationToken);

        return (ShowcasePlanCut.Inside(showcase.Items, products, limit), products);
    }
}
