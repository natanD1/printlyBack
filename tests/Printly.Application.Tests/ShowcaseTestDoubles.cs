using Printly.Application.Services;
using Printly.Domain.Entities;
using Printly.Domain.Enums;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Tests;

/// <summary>Vitrines em memoria; as projecoes de produto leem do <see cref="FakeProductRepository"/> compartilhado.</summary>
internal sealed class FakeShowcaseRepository : IShowcaseRepository
{
    private readonly FakeProductRepository _products;

    public FakeShowcaseRepository(FakeProductRepository products)
    {
        _products = products;
    }

    public List<Showcase> Items { get; } = new();

    /// <summary>Quantas vezes o slug foi consultado (slug invalido nao deve chegar aqui).</summary>
    public int SlugLookups { get; private set; }

    public Task<Showcase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(s => s.Id == id));

    public Task<Showcase?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(s => s.UserId == userId));

    public Task<Showcase?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        SlugLookups++;
        return Task.FromResult(Items.FirstOrDefault(s => s.Slug == slug));
    }

    public Task<List<Showcase>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.OrderBy(s => s.Slug, StringComparer.Ordinal).ToList());

    public Task<bool> SlugExistsAsync(string slug, Guid? exceptShowcaseId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(s => s.Slug == slug && (exceptShowcaseId == null || s.Id != exceptShowcaseId)));

    public Task<int> CountVisibleProducedItemsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items
            .Where(s => s.UserId == userId)
            .SelectMany(s => s.Items)
            .Count(i => i.IsVisible && _products.Items.Any(p => p.Id == i.ProductId && p.UserId == userId && p.IsProduced)));

    public Task<List<ShowcaseProductSummary>> GetProductSummariesAsync(
        Guid userId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.Items
            .Where(p => p.UserId == userId && productIds.Contains(p.Id))
            .Select(ToSummary)
            .ToList());

    public Task<List<ShowcaseProductSummary>> GetAvailableProductsAsync(Guid userId, string? search, CancellationToken cancellationToken = default)
    {
        var inShowcase = Items.Where(s => s.UserId == userId).SelectMany(s => s.Items).Select(i => i.ProductId).ToHashSet();

        return Task.FromResult(_products.Items
            .Where(p => p.UserId == userId && p.IsProduced && !inShowcase.Contains(p.Id))
            .Where(p => string.IsNullOrEmpty(search) || p.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.ProductName, StringComparer.Ordinal)
            .Select(ToSummary)
            .ToList());
    }

    public Task<string?> GetProductPhotoAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.Items.FirstOrDefault(p => p.Id == productId && p.UserId == userId)?.ProductPhoto);

    public Task AddAsync(Showcase showcase, CancellationToken cancellationToken = default)
    {
        Items.Add(showcase);
        return Task.CompletedTask;
    }

    private static ShowcaseProductSummary ToSummary(Product p) =>
        new(p.Id, p.ProductName, p.UnitSalePrice, !string.IsNullOrEmpty(p.ProductPhoto), p.IsProduced);
}

internal sealed class InMemoryCompanyProfileRepository : ICompanyProfileRepository
{
    public List<CompanyProfile> Items { get; } = new();

    public Task<CompanyProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(c => c.UserId == userId));

    public Task AddAsync(CompanyProfile companyProfile, CancellationToken cancellationToken = default)
    {
        Items.Add(companyProfile);
        return Task.CompletedTask;
    }
}

/// <summary>Conta os SaveChanges e pode simular o indice unico violado (MySQL 1062 traduzido pela Infrastructure).</summary>
internal sealed class RecordingUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public bool ThrowUniqueViolation { get; set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowUniqueViolation)
            throw new UniqueConstraintException("Registro duplicado.", new Exception("1062"));

        SaveCount++;
        return Task.FromResult(1);
    }
}

internal static class TestImages
{
    public static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01 };
    public static readonly byte[] JpegBytes = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

    public static string Png => Convert.ToBase64String(PngBytes);
    public static string Jpeg => Convert.ToBase64String(JpegBytes);
    public static string Svg => Convert.ToBase64String("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"u8.ToArray());
    public static string Garbage => Convert.ToBase64String(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 });
}

/// <summary>Vitrine com o EntitlementService real (sem banco) por cima do <see cref="PlanTestContext"/>.</summary>
internal sealed class ShowcaseTestContext
{
    public PlanTestContext Plan { get; } = new();
    public InMemoryCompanyProfileRepository Companies { get; } = new();
    public RecordingUnitOfWork UnitOfWork { get; } = new();

    public FakeShowcaseRepository Showcases => Plan.Showcases;
    public FakeProductRepository Products => Plan.Products;

    public ShowcaseService Service() => new(Showcases, Companies, Plan.Users, Plan.Entitlements, UnitOfWork);

    public CompanyProfileService CompanyService() => new(Companies, Plan.Entitlements, Showcases, UnitOfWork);

    public PublicShowcaseService Public() => new(Showcases, Companies, Plan.Entitlements);

    /// <summary>Usuario num plano com a vitrine ligada e <paramref name="limit"/> itens (nulo = ilimitado).</summary>
    public User AddUser(int? limit = null, bool showcaseEnabled = true, bool logo = false, bool removeBranding = false, bool admin = false)
    {
        var plan = Plan.AddPlan(
            TestPlans.Feature(PlanEntitlementKey.ShowcaseEnabled, showcaseEnabled),
            TestPlans.Limit(PlanEntitlementKey.MaxShowcaseItems, limit),
            TestPlans.Feature(PlanEntitlementKey.CompanyLogoInQuote, logo),
            TestPlans.Feature(PlanEntitlementKey.RemoveBrandingFooter, removeBranding));

        return Plan.AddUser(plan, admin);
    }

    /// <summary>Troca o plano do usuario (downgrade/upgrade) mantendo os dados.</summary>
    public void ChangePlan(User user, int? limit, bool showcaseEnabled = true)
    {
        var plan = Plan.AddPlan(
            TestPlans.Feature(PlanEntitlementKey.ShowcaseEnabled, showcaseEnabled),
            TestPlans.Limit(PlanEntitlementKey.MaxShowcaseItems, limit));

        user.ChangePlan(plan.Id);
    }

    public CompanyProfile AddCompany(
        Guid userId,
        string name = "Ateliê 3D",
        string? logo = null,
        string? phone = null,
        string? email = null,
        string? whatsApp = null,
        string? instagram = null,
        string? website = null)
    {
        var company = new CompanyProfile(userId, name, logo, phone, email, whatsApp, instagram, website);
        Companies.Items.Add(company);
        return company;
    }

    public Product AddProduct(Guid userId, string name = "Vaso", bool produced = true, string? photo = null, decimal salePrice = 51m, int quantity = 2)
    {
        var product = new Product(name, photo, 1m, quantity, 1, Guid.NewGuid(), userId);
        product.SetPricing(10m, salePrice);

        if (produced)
            product.SetProduced(true);

        Products.Items.Add(product);
        return product;
    }

    public Showcase AddShowcase(Guid userId, string slug, params Product[] products)
    {
        var showcase = new Showcase(userId, slug);
        showcase.AddItems(products.Select(p => p.Id));
        Showcases.Items.Add(showcase);
        return showcase;
    }
}
