using PrintFlow.Application.DTOs.Products;
using PrintFlow.Application.Interfaces;
using PrintFlow.Domain.Entities;
using PrintFlow.Domain.Exceptions;
using PrintFlow.Domain.Interfaces;

namespace PrintFlow.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IFilamentRepository _filamentRepository;
    private readonly IUserSettingsRepository _userSettingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(
        IProductRepository productRepository,
        IFilamentRepository filamentRepository,
        IUserSettingsRepository userSettingsRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _filamentRepository = filamentRepository;
        _userSettingsRepository = userSettingsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProductDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllByUserIdAsync(userId, cancellationToken);
        return await ToDtosAsync(products, userId, cancellationToken);
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var product = await GetOwnedProductAsync(id, userId, cancellationToken);
        return await ToDtoAsync(product, userId, cancellationToken);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var product = new Product(request.ProductName, request.ProductPhotoBase64, request.TotalHours, userId);

        var ownedFilaments = await ValidateAndBuildFilamentsAsync(request.Filaments, userId, cancellationToken);
        DeductStockOrThrow(ownedFilaments, request.Filaments);
        product.SetFilaments(request.Filaments.Select(f => (f.FilamentId, f.GramsUsed)));

        await ApplyPricingAsync(product, ownedFilaments, request.SalePriceOverride, userId, cancellationToken);

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(product, userId, cancellationToken);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var product = await GetOwnedProductAsync(id, userId, cancellationToken);

        var productPhoto = string.IsNullOrWhiteSpace(request.ProductPhotoBase64)
            ? product.ProductPhoto
            : request.ProductPhotoBase64;

        product.Update(request.ProductName, productPhoto, request.TotalHours);

        if (request.Filaments is not null)
        {
            var oldFilamentIds = product.Filaments.Select(pf => pf.FilamentId).Distinct().ToList();
            var oldFilaments = await GetFilamentsByIdsAsync(oldFilamentIds, userId, cancellationToken);

            foreach (var oldLink in product.Filaments)
                if (oldFilaments.TryGetValue(oldLink.FilamentId, out var oldFilament))
                    oldFilament.RestoreStock(oldLink.GramsUsed / 1000m);

            var ownedFilaments = await ValidateAndBuildFilamentsAsync(request.Filaments, userId, cancellationToken);
            DeductStockOrThrow(ownedFilaments, request.Filaments);
            product.SetFilaments(request.Filaments.Select(f => (f.FilamentId, f.GramsUsed)));
        }

        var currentFilamentIds = product.Filaments.Select(pf => pf.FilamentId).Distinct().ToList();
        var filamentsForPricing = await GetFilamentsByIdsAsync(currentFilamentIds, userId, cancellationToken);

        await ApplyPricingAsync(product, filamentsForPricing, request.SalePriceOverride, userId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(product, userId, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var product = await GetOwnedProductAsync(id, userId, cancellationToken);

        _productRepository.Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyPricingAsync(
        Product product,
        Dictionary<Guid, Filament> filamentsById,
        decimal? salePriceOverride,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var settings = await _userSettingsRepository.GetOrCreateAsync(userId, cancellationToken);

        var filamentCost = product.Filaments
            .Where(pf => filamentsById.ContainsKey(pf.FilamentId))
            .Sum(pf => (pf.GramsUsed / 1000m) * filamentsById[pf.FilamentId].FilamentPrice);

        var energyCost = product.TotalHours * (settings.AveragePowerWatts / 1000m) * settings.KwhPrice;
        var costPrice = filamentCost + energyCost;
        var suggestedSalePrice = costPrice * (1 + (settings.DefaultProfitMarginPercentage / 100m));

        product.SetPricing(costPrice, salePriceOverride ?? suggestedSalePrice);
    }

    private static void DeductStockOrThrow(Dictionary<Guid, Filament> filamentsById, List<ProductFilamentRequest> filaments)
    {
        foreach (var item in filaments)
        {
            var filament = filamentsById[item.FilamentId];
            var kilograms = item.GramsUsed / 1000m;

            if (filament.Weight < kilograms)
                throw new DomainException($"Estoque insuficiente de '{filament.Name}': disponivel {filament.Weight * 1000}g, necessario {item.GramsUsed}g.");
        }

        foreach (var item in filaments)
            filamentsById[item.FilamentId].DeductStock(item.GramsUsed / 1000m);
    }

    private async Task<Dictionary<Guid, Filament>> ValidateAndBuildFilamentsAsync(List<ProductFilamentRequest> filaments, Guid userId, CancellationToken cancellationToken)
    {
        var filamentIds = filaments.Select(f => f.FilamentId).Distinct().ToList();

        if (filamentIds.Count == 0)
            return new Dictionary<Guid, Filament>();

        var ownedFilaments = await _filamentRepository.GetByIdsAsync(filamentIds, userId, cancellationToken);

        if (ownedFilaments.Count != filamentIds.Count)
            throw new DomainException("Um ou mais filamentos informados nao foram encontrados.");

        return ownedFilaments.ToDictionary(f => f.Id);
    }

    private async Task<Product> GetOwnedProductAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);

        if (product is null || product.UserId != userId)
            throw new DomainException("Produto nao encontrado.");

        return product;
    }

    private async Task<List<ProductDto>> ToDtosAsync(List<Product> products, Guid userId, CancellationToken cancellationToken)
    {
        var filamentIds = products.SelectMany(p => p.Filaments.Select(f => f.FilamentId)).Distinct().ToList();
        var filaments = await GetFilamentsByIdsAsync(filamentIds, userId, cancellationToken);
        var settings = await _userSettingsRepository.GetOrCreateAsync(userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return products.Select(p => ToDto(p, filaments, settings)).ToList();
    }

    private async Task<ProductDto> ToDtoAsync(Product product, Guid userId, CancellationToken cancellationToken)
    {
        var filamentIds = product.Filaments.Select(f => f.FilamentId).Distinct().ToList();
        var filaments = await GetFilamentsByIdsAsync(filamentIds, userId, cancellationToken);
        var settings = await _userSettingsRepository.GetOrCreateAsync(userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(product, filaments, settings);
    }

    private async Task<Dictionary<Guid, Filament>> GetFilamentsByIdsAsync(List<Guid> filamentIds, Guid userId, CancellationToken cancellationToken)
    {
        if (filamentIds.Count == 0)
            return new Dictionary<Guid, Filament>();

        var filaments = await _filamentRepository.GetByIdsAsync(filamentIds, userId, cancellationToken);
        return filaments.ToDictionary(f => f.Id);
    }

    private static ProductDto ToDto(Product product, Dictionary<Guid, Filament> filaments, UserSettings settings)
    {
        var productFilaments = product.Filaments
            .Where(pf => filaments.ContainsKey(pf.FilamentId))
            .Select(pf =>
            {
                var filament = filaments[pf.FilamentId];
                return new ProductFilamentDto(filament.Id, filament.Name, filament.Brand, filament.ColorHex, pf.GramsUsed);
            })
            .ToList();

        var suggestedSalePrice = product.CostPrice * (1 + (settings.DefaultProfitMarginPercentage / 100m));

        return new ProductDto(
            product.Id,
            product.ProductName,
            product.ProductPhoto,
            product.TotalHours,
            product.TotalFilament,
            product.CostPrice,
            product.SalePrice,
            suggestedSalePrice,
            productFilaments,
            product.CreatedAt,
            product.UpdatedAt);
    }
}
