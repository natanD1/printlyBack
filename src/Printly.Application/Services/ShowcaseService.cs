using Printly.Application.DTOs.Showcases;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Enums;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

public class ShowcaseService : IShowcaseService
{
    public const int MaxItemsPerRequest = 100;
    public const int MaxSearchLength = 100;

    private const string ShowcaseNotFound = "Vitrine nao encontrada.";
    private const string ProductNotFound = "Produto nao encontrado.";

    /// <summary>Teto de tentativas de sufixo (-2, -3...) ao gerar o slug.</summary>

    private readonly IShowcaseRepository _showcaseRepository;
    private readonly ICompanyProfileRepository _companyProfileRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEntitlementService _entitlementService;
    private readonly IUnitOfWork _unitOfWork;

    public ShowcaseService(
        IShowcaseRepository showcaseRepository,
        ICompanyProfileRepository companyProfileRepository,
        IUserRepository userRepository,
        IEntitlementService entitlementService,
        IUnitOfWork unitOfWork)
    {
        _showcaseRepository = showcaseRepository;
        _companyProfileRepository = companyProfileRepository;
        _userRepository = userRepository;
        _entitlementService = entitlementService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShowcaseStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var showcase = await _showcaseRepository.GetByUserIdAsync(userId, cancellationToken);

        if (showcase is null)
            return new NoShowcaseDto(await PlanAllowsAsync(userId, cancellationToken));

        return await ToDtoAsync(showcase, cancellationToken);
    }

    /// <summary>Idempotente: se ja existe, devolve a atual. Slug sai do nome da empresa e acompanha as trocas de nome (CompanyProfileService).</summary>
    public async Task<ShowcaseDto> CreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureEnabledAsync(userId, cancellationToken);

        var existing = await _showcaseRepository.GetByUserIdAsync(userId, cancellationToken);

        if (existing is not null)
            return await ToDtoAsync(existing, cancellationToken);

        var company = await _companyProfileRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Cadastre o nome da empresa em Configuracoes antes de criar a vitrine.");

        var showcase = new Showcase(userId, await ShowcaseSlugResolver.ResolveFreeAsync(_showcaseRepository, company.Name, null, cancellationToken));

        await _showcaseRepository.AddAsync(showcase, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException)
        {
            // Corrida: outro POST do mesmo usuario criou antes (indice unico em UserId) ou o slug foi pego agora.
            var created = await _showcaseRepository.GetByUserIdAsync(userId, cancellationToken);

            if (created is not null && created.Id != showcase.Id)
                return await ToDtoAsync(created, cancellationToken);

            throw new DomainException(ShowcaseSlug.InUseMessage);
        }

        return await ToDtoAsync(showcase, cancellationToken);
    }

    public async Task<ShowcaseDto> SetPublishedAsync(Guid userId, bool isPublished, CancellationToken cancellationToken = default)
    {
        await EnsureEnabledAsync(userId, cancellationToken);
        var showcase = await GetOwnShowcaseAsync(userId, ShowcaseNotFound, cancellationToken);

        showcase.SetPublished(isPublished);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(showcase, cancellationToken);
    }

    /// <summary>
    /// Lote atomico: valida tudo (dono, produzido, limite com os novos) antes de mexer na vitrine e grava num
    /// unico SaveChanges. Ja presentes sao ignorados; novos entram no fim, visiveis.
    /// </summary>
    public async Task<ShowcaseDto> AddItemsAsync(Guid userId, IReadOnlyList<Guid>? productIds, CancellationToken cancellationToken = default)
    {
        if (productIds is null || productIds.Count == 0 || productIds.Count > MaxItemsPerRequest)
            throw new DomainException($"Informe de 1 a {MaxItemsPerRequest} produtos.");

        if (productIds.Distinct().Count() != productIds.Count)
            throw new DomainException("A lista de produtos tem itens repetidos.");

        await EnsureEnabledAsync(userId, cancellationToken);
        var showcase = await GetOwnShowcaseAsync(userId, "Crie a vitrine antes de adicionar produtos.", cancellationToken);

        var products = await _showcaseRepository.GetProductSummariesAsync(userId, productIds.ToList(), cancellationToken);

        // Alheio e inexistente respondem igual: nao vaza que o id existe em outra conta.
        if (products.Count != productIds.Count)
            throw new NotFoundException(ProductNotFound);

        if (products.Any(p => !p.IsProduced))
            throw new DomainException("Só produtos produzidos podem ir para a vitrine.");

        var newIds = productIds.Where(id => showcase.GetItem(id) is null).ToList();

        if (newIds.Count > 0)
        {
            await _entitlementService.EnsureWithinLimitAsync(userId, PlanEntitlementKey.MaxShowcaseItems, newIds.Count, cancellationToken);

            showcase.AddItems(newIds);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (UniqueConstraintException)
            {
                // Corrida: outro POST adicionou o mesmo produto agora (indice unico vitrine+produto). Nada foi gravado.
                throw new DomainException("A vitrine mudou enquanto voce adicionava. Atualize a pagina e tente de novo.");
            }
        }

        return await ToDtoAsync(showcase, cancellationToken);
    }

    public async Task<ShowcaseDto> ReorderItemsAsync(Guid userId, IReadOnlyList<Guid>? productIds, CancellationToken cancellationToken = default)
    {
        await EnsureEnabledAsync(userId, cancellationToken);
        var showcase = await GetOwnShowcaseAsync(userId, ShowcaseNotFound, cancellationToken);

        showcase.Reorder(productIds ?? Array.Empty<Guid>());
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(showcase, cancellationToken);
    }

    /// <summary>Tornar visivel um item de produto produzido ocupa uma vaga: checa o limite.</summary>
    public async Task<ShowcaseDto> SetItemVisibilityAsync(Guid userId, Guid productId, bool isVisible, CancellationToken cancellationToken = default)
    {
        await EnsureEnabledAsync(userId, cancellationToken);
        var showcase = await GetOwnShowcaseAsync(userId, ShowcaseNotFound, cancellationToken);
        var item = showcase.GetItem(productId) ?? throw new NotFoundException(ProductNotFound);

        if (isVisible && !item.IsVisible)
        {
            var product = (await _showcaseRepository.GetProductSummariesAsync(userId, new[] { productId }, cancellationToken))
                .FirstOrDefault();

            if (product?.IsProduced == true)
                await _entitlementService.EnsureWithinLimitAsync(userId, PlanEntitlementKey.MaxShowcaseItems, 1, cancellationToken);
        }

        item.SetVisible(isVisible);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(showcase, cancellationToken);
    }

    /// <summary>Permitido sem o recurso no plano (o usuario sempre pode enxugar a vitrine).</summary>
    public async Task RemoveItemAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var showcase = await GetOwnShowcaseAsync(userId, ShowcaseNotFound, cancellationToken);

        if (!showcase.RemoveItem(productId))
            throw new NotFoundException(ProductNotFound);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Produzidos fora da vitrine, por nome. Nao aplica a janela ProductHistoryMonths.</summary>
    public async Task<List<AvailableProductDto>> GetAvailableProductsAsync(Guid userId, string? search, CancellationToken cancellationToken = default)
    {
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        if (term is { Length: > MaxSearchLength })
            throw new DomainException($"Busca deve ter no maximo {MaxSearchLength} caracteres.");

        var products = await _showcaseRepository.GetAvailableProductsAsync(userId, term, cancellationToken);

        return products.Select(p => new AvailableProductDto(p.Id, p.Name, p.UnitSalePrice, p.HasPhoto)).ToList();
    }

    public async Task<ImageContent?> GetProductPhotoAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default) =>
        ImageContent.TryDecode(await _showcaseRepository.GetProductPhotoAsync(userId, productId, cancellationToken));

    public async Task<List<AdminShowcaseDto>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var showcases = await _showcaseRepository.GetAllAsync(cancellationToken);
        var owners = (await _userRepository.GetByIdsAsync(showcases.Select(s => s.UserId).Distinct().ToList(), cancellationToken))
            .ToDictionary(u => u.Id);

        return showcases
            .OrderBy(s => s.Slug, StringComparer.Ordinal)
            .Select(s => ToAdminDto(s, owners.GetValueOrDefault(s.UserId)))
            .ToList();
    }

    public async Task<AdminShowcaseDto> SetBlockedAsync(Guid showcaseId, bool isBlocked, CancellationToken cancellationToken = default)
    {
        var showcase = await _showcaseRepository.GetByIdAsync(showcaseId, cancellationToken)
            ?? throw new NotFoundException(ShowcaseNotFound);

        showcase.SetBlocked(isBlocked);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var owner = await _userRepository.GetByIdAsync(showcase.UserId, cancellationToken);
        return ToAdminDto(showcase, owner);
    }

    private Task EnsureEnabledAsync(Guid userId, CancellationToken cancellationToken) =>
        _entitlementService.EnsureFeatureAsync(userId, PlanEntitlementKey.ShowcaseEnabled, cancellationToken);

    private Task<bool> PlanAllowsAsync(Guid userId, CancellationToken cancellationToken) =>
        _entitlementService.IsFeatureEnabledAsync(userId, PlanEntitlementKey.ShowcaseEnabled, cancellationToken);

    private async Task<Showcase> GetOwnShowcaseAsync(Guid userId, string notFoundMessage, CancellationToken cancellationToken) =>
        await _showcaseRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(notFoundMessage);

    private async Task<ShowcaseDto> ToDtoAsync(Showcase showcase, CancellationToken cancellationToken)
    {
        var userId = showcase.UserId;
        var products = (await _showcaseRepository.GetProductSummariesAsync(
                userId, showcase.Items.Select(i => i.ProductId).ToList(), cancellationToken))
            .ToDictionary(p => p.Id);

        var limit = await _entitlementService.GetLimitAsync(userId, PlanEntitlementKey.MaxShowcaseItems, cancellationToken);
        var used = await _showcaseRepository.CountVisibleProducedItemsAsync(userId, cancellationToken);
        var inside = ShowcasePlanCut.Inside(showcase.Items, products, limit).Select(i => i.Id).ToHashSet();

        var items = showcase.Items
            .Where(i => products.ContainsKey(i.ProductId))
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.CreatedAt)
            .Select(i =>
            {
                var product = products[i.ProductId];
                var outsidePlan = ShowcasePlanCut.IsEligible(i, products) && !inside.Contains(i.Id);
                return new ShowcaseItemDto(
                    i.ProductId, product.Name, product.UnitSalePrice, product.HasPhoto,
                    i.SortOrder, i.IsVisible, product.IsProduced, outsidePlan);
            })
            .ToList();

        return new ShowcaseDto(
            await PlanAllowsAsync(userId, cancellationToken),
            showcase.IsPublished,
            showcase.IsBlocked,
            showcase.Slug,
            limit,
            used,
            items);
    }

    private static AdminShowcaseDto ToAdminDto(Showcase showcase, User? owner) => new(
        showcase.Id,
        showcase.UserId,
        showcase.Slug,
        owner?.Name ?? string.Empty,
        owner?.Email ?? string.Empty,
        showcase.IsPublished,
        showcase.IsBlocked,
        showcase.Items.Count);
}
