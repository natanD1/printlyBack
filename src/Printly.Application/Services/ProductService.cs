using Printly.Application.DTOs.Products;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IFilamentRepository _filamentRepository;
    private readonly IFilamentBrandRepository _filamentBrandRepository;
    private readonly IUserSettingsRepository _userSettingsRepository;
    private readonly IPrinterRepository _printerRepository;
    private readonly IPrinterModelRepository _printerModelRepository;
    private readonly IPrinterBrandRepository _printerBrandRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(
        IProductRepository productRepository,
        IFilamentRepository filamentRepository,
        IFilamentBrandRepository filamentBrandRepository,
        IUserSettingsRepository userSettingsRepository,
        IPrinterRepository printerRepository,
        IPrinterModelRepository printerModelRepository,
        IPrinterBrandRepository printerBrandRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _filamentRepository = filamentRepository;
        _filamentBrandRepository = filamentBrandRepository;
        _userSettingsRepository = userSettingsRepository;
        _printerRepository = printerRepository;
        _printerModelRepository = printerModelRepository;
        _printerBrandRepository = printerBrandRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProductDto>> GetAllAsync(Guid userId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        if (from is { } start && to is { } end && start >= end)
            throw new DomainException("Periodo invalido: a data inicial deve ser anterior a final.");

        var products = await _productRepository.GetAllByUserIdAsync(userId, from, to, cancellationToken);
        return await ToDtosAsync(products, userId, cancellationToken);
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var product = await GetOwnedProductAsync(id, userId, cancellationToken);
        return await ToDtoAsync(product, userId, cancellationToken);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var printer = await GetOwnedPrinterAsync(request.PrinterId, userId, cancellationToken);
        var product = new Product(request.ProductName, request.ProductPhotoBase64, request.TotalHours, request.Quantity, request.PiecesPerPrint, printer.Id, userId);

        var ownedFilaments = await ValidateAndBuildFilamentsAsync(request.Filaments, userId, cancellationToken);
        DeductStockOrThrow(ownedFilaments, request.Filaments, product.PrintRuns);
        product.SetFilaments(request.Filaments.Select(f => (f.FilamentId, f.GramsUsed)));

        await ApplyPricingAsync(product, ownedFilaments, printer, request.SalePriceOverride, userId, cancellationToken);

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(product, userId, cancellationToken);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var product = await GetOwnedProductAsync(id, userId, cancellationToken);
        var printer = await GetOwnedPrinterAsync(request.PrinterId, userId, cancellationToken);

        var productPhoto = string.IsNullOrWhiteSpace(request.ProductPhotoBase64)
            ? product.ProductPhoto
            : request.ProductPhotoBase64;

        var oldPrintRuns = product.PrintRuns;
        product.Update(request.ProductName, productPhoto, request.TotalHours, request.Quantity, request.PiecesPerPrint, printer.Id);

        if (request.Filaments is not null || product.PrintRuns != oldPrintRuns)
        {
            var oldFilamentIds = product.Filaments.Select(pf => pf.FilamentId).Distinct().ToList();
            var oldFilaments = await GetFilamentsByIdsAsync(oldFilamentIds, userId, cancellationToken);

            foreach (var oldLink in product.Filaments)
                if (oldFilaments.TryGetValue(oldLink.FilamentId, out var oldFilament))
                    oldFilament.RestoreStock(oldLink.GramsUsed * oldPrintRuns / 1000m);

            var newFilaments = request.Filaments
                ?? product.Filaments.Select(pf => new ProductFilamentRequest(pf.FilamentId, pf.GramsUsed)).ToList();

            var ownedFilaments = await ValidateAndBuildFilamentsAsync(newFilaments, userId, cancellationToken);
            DeductStockOrThrow(ownedFilaments, newFilaments, product.PrintRuns);

            if (request.Filaments is not null)
                product.SetFilaments(request.Filaments.Select(f => (f.FilamentId, f.GramsUsed)));
        }

        var currentFilamentIds = product.Filaments.Select(pf => pf.FilamentId).Distinct().ToList();
        var filamentsForPricing = await GetFilamentsByIdsAsync(currentFilamentIds, userId, cancellationToken);

        await ApplyPricingAsync(product, filamentsForPricing, printer, request.SalePriceOverride, userId, cancellationToken);

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
        Printer printer,
        decimal? salePriceOverride,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var settings = await _userSettingsRepository.GetOrCreateAsync(userId, cancellationToken);

        var filaments = product.Filaments
            .Where(pf => filamentsById.ContainsKey(pf.FilamentId))
            .Select(pf => (pf.GramsUsed, filamentsById[pf.FilamentId].FilamentPrice));

        var costPrice = ProductPricingCalculator.CalculateCost(filaments, product.TotalHours, product.PrintRuns, printer, settings.KwhPrice);
        var salePrice = salePriceOverride is { } unitPrice
            ? unitPrice * product.Quantity
            : ProductPricingCalculator.SuggestSalePrice(costPrice, printer);

        product.SetPricing(costPrice, salePrice);
    }

    private static void DeductStockOrThrow(Dictionary<Guid, Filament> filamentsById, List<ProductFilamentRequest> filaments, decimal printRuns)
    {
        foreach (var item in filaments)
        {
            var filament = filamentsById[item.FilamentId];
            var gramsNeeded = item.GramsUsed * printRuns;

            if (filament.Weight < gramsNeeded / 1000m)
            {
                var breakdown = printRuns != 1m ? $" ({item.GramsUsed:0.##}g x {printRuns:0.##} impressoes)" : string.Empty;
                throw new DomainException($"Estoque insuficiente de '{filament.Name}': disponivel {filament.Weight * 1000:0.##}g, necessario {gramsNeeded:0.##}g{breakdown}.");
            }
        }

        foreach (var item in filaments)
            filamentsById[item.FilamentId].DeductStock(item.GramsUsed * printRuns / 1000m);
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

    private async Task<Printer> GetOwnedPrinterAsync(Guid? printerId, Guid userId, CancellationToken cancellationToken)
    {
        if (printerId is not { } id)
            throw new DomainException("Selecione uma impressora.");

        var printer = await _printerRepository.GetByIdAsync(id, cancellationToken);

        if (printer is null || printer.UserId != userId)
            throw new DomainException("Impressora nao encontrada.");

        return printer;
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
        var brandNames = await GetBrandNamesAsync(filaments.Values, cancellationToken);
        var printers = await GetPrintersAsync(products, userId, cancellationToken);

        return products.Select(p => ToDto(p, filaments, brandNames, printers)).ToList();
    }

    private async Task<ProductDto> ToDtoAsync(Product product, Guid userId, CancellationToken cancellationToken) =>
        (await ToDtosAsync([product], userId, cancellationToken))[0];

    /// <summary>Impressoras dos produtos + nome exibido de cada uma, buscadas em lote.</summary>
    private async Task<Dictionary<Guid, (Printer Printer, string Name)>> GetPrintersAsync(List<Product> products, Guid userId, CancellationToken cancellationToken)
    {
        var printerIds = products.Where(p => p.PrinterId.HasValue).Select(p => p.PrinterId!.Value).Distinct().ToList();

        if (printerIds.Count == 0)
            return new Dictionary<Guid, (Printer, string)>();

        var printers = await _printerRepository.GetByIdsAsync(printerIds, userId, cancellationToken);
        var catalog = await PrinterCatalog.LoadAsync(_printerModelRepository, _printerBrandRepository, cancellationToken);

        return printers.ToDictionary(p => p.Id, p => (p, catalog.DisplayName(p)));
    }

    private async Task<Dictionary<Guid, Filament>> GetFilamentsByIdsAsync(List<Guid> filamentIds, Guid userId, CancellationToken cancellationToken)
    {
        if (filamentIds.Count == 0)
            return new Dictionary<Guid, Filament>();

        var filaments = await _filamentRepository.GetByIdsAsync(filamentIds, userId, cancellationToken);
        return filaments.ToDictionary(f => f.Id);
    }

    private async Task<Dictionary<Guid, string>> GetBrandNamesAsync(IEnumerable<Filament> filaments, CancellationToken cancellationToken)
    {
        var brandIds = filaments.Select(f => f.BrandId).Distinct().ToList();

        if (brandIds.Count == 0)
            return new Dictionary<Guid, string>();

        var brands = await _filamentBrandRepository.GetAllAsync(cancellationToken);
        return brands.Where(b => brandIds.Contains(b.Id)).ToDictionary(b => b.Id, b => b.NameBrand);
    }

    private static ProductDto ToDto(
        Product product,
        Dictionary<Guid, Filament> filaments,
        Dictionary<Guid, string> brandNames,
        Dictionary<Guid, (Printer Printer, string Name)> printers)
    {
        var productFilaments = product.Filaments
            .Where(pf => filaments.ContainsKey(pf.FilamentId))
            .Select(pf =>
            {
                var filament = filaments[pf.FilamentId];
                var brandName = brandNames.GetValueOrDefault(filament.BrandId, string.Empty);
                return new ProductFilamentDto(filament.Id, filament.Name, brandName, filament.ColorHex, pf.GramsUsed);
            })
            .ToList();

        var printer = product.PrinterId is { } printerId && printers.TryGetValue(printerId, out var found) ? found : default;

        // Produto legado (sem impressora) nao tem margem pra aplicar: o sugerido e o preco salvo.
        var suggestedSalePrice = printer.Printer is not null
            ? ProductPricingCalculator.SuggestSalePrice(product.CostPrice, printer.Printer)
            : product.SalePrice;

        return new ProductDto(
            product.Id,
            product.ProductName,
            product.ProductPhoto,
            product.TotalHours,
            product.Quantity,
            product.PiecesPerPrint,
            product.TotalHours * product.PrintRuns,
            product.TotalFilament,
            product.TotalFilament * product.PrintRuns,
            product.CostPrice,
            product.SalePrice,
            product.SalePrice / product.Quantity,
            suggestedSalePrice,
            printer.Printer?.Id,
            printer.Name,
            productFilaments,
            product.CreatedAt,
            product.UpdatedAt);
    }
}
