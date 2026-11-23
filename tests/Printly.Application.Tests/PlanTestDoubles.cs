using Printly.Application.DTOs.Plans;
using Printly.Application.Interfaces;
using Printly.Application.Services;
using Printly.Domain.Entities;
using Printly.Domain.Enums;
using Printly.Domain.Interfaces;

namespace Printly.Application.Tests;

internal static class TestPlans
{
    public static Plan Create(string name = "Basico", bool isDefault = false, bool isActive = true, params (PlanEntitlementKey Key, int? IntValue, bool? BoolValue)[] entitlements)
    {
        var plan = new Plan(name, null, 0m, isActive, 0);
        plan.SetEntitlements(entitlements);

        if (isDefault)
            plan.MarkAsDefault();

        return plan;
    }

    public static (PlanEntitlementKey, int?, bool?) Limit(PlanEntitlementKey key, int? value) => (key, value, null);

    public static (PlanEntitlementKey, int?, bool?) Feature(PlanEntitlementKey key, bool value) => (key, null, value);

    /// <summary>CreatedAt tem setter protegido: os testes de mes/historico precisam controlar o instante.</summary>
    public static T WithCreatedAt<T>(this T entity, DateTime createdAtUtc) where T : BaseEntity
    {
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.CreatedAt))!.SetValue(entity, createdAtUtc);
        return entity;
    }
}

internal sealed class FakePlanRepository : IPlanRepository
{
    public List<Plan> Items { get; } = new();

    /// <summary>Usuarios usados nas contagens por plano (opcional).</summary>
    public FakeUserRepository? Users { get; set; }

    public static FakePlanRepository WithDefaultPlan()
    {
        var repository = new FakePlanRepository();
        repository.Items.Add(TestPlans.Create("Free", isDefault: true));
        return repository;
    }

    public Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(p => p.Id == id));

    public Task<List<Plan>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToList());

    public Task<Plan?> GetDefaultAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(p => p.IsDefault));

    public Task<bool> NameExistsAsync(string name, Guid? exceptId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(p =>
            string.Equals(p.Name, name.Trim(), StringComparison.OrdinalIgnoreCase) && (exceptId == null || p.Id != exceptId)));

    public Task<int> CountUsersAsync(Guid planId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users?.Items.Count(u => u.PlanId == planId) ?? 0);

    public Task<Dictionary<Guid, int>> CountUsersByPlanAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult((Users?.Items ?? new List<User>()).GroupBy(u => u.PlanId).ToDictionary(g => g.Key, g => g.Count()));

    public Task AddAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        Items.Add(plan);
        return Task.CompletedTask;
    }

    public void Remove(Plan plan) => Items.Remove(plan);
}

internal sealed class FakePrinterRepository : IPrinterRepository
{
    public List<Printer> Items { get; } = new();

    public Task<Printer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(p => p.Id == id));

    public Task<List<Printer>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Where(p => p.UserId == userId).ToList());

    public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Count(p => p.UserId == userId));

    public Task<List<Printer>> GetByIdsAsync(List<Guid> ids, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Where(p => ids.Contains(p.Id) && p.UserId == userId).ToList());

    public Task AddAsync(Printer printer, CancellationToken cancellationToken = default)
    {
        Items.Add(printer);
        return Task.CompletedTask;
    }

    public void Remove(Printer printer) => Items.Remove(printer);
}

internal sealed class FakeFilamentRepository : IFilamentRepository
{
    public List<Filament> Items { get; } = new();

    public Task<Filament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(f => f.Id == id));

    public Task<List<Filament>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Where(f => f.UserId == userId).ToList());

    public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Count(f => f.UserId == userId));

    public Task<List<Filament>> GetByIdsAsync(List<Guid> ids, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Where(f => ids.Contains(f.Id) && f.UserId == userId).ToList());

    public Task AddAsync(Filament filament, CancellationToken cancellationToken = default)
    {
        Items.Add(filament);
        return Task.CompletedTask;
    }

    public void Remove(Filament filament) => Items.Remove(filament);
}

internal sealed class FakeProductRepository : IProductRepository
{
    public List<Product> Items { get; } = new();

    /// <summary>Ultimo periodo pedido em GetAllByUserIdAsync (pra conferir o "from" aplicado pelo plano).</summary>
    public (DateTime? From, DateTime? To)? LastQuery { get; private set; }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(p => p.Id == id));

    public Task<List<Product>> GetAllByUserIdAsync(Guid userId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        LastQuery = (from, to);
        return Task.FromResult(Items
            .Where(p => p.UserId == userId && (from == null || p.CreatedAt >= from) && (to == null || p.CreatedAt < to))
            .ToList());
    }

    public Task<int> CountCreatedByUserIdAsync(Guid userId, DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Count(p => p.UserId == userId && p.CreatedAt >= from && p.CreatedAt < to));

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        Items.Add(product);
        return Task.CompletedTask;
    }

    public void Remove(Product product) => Items.Remove(product);
}

internal sealed class FakePrinterModelRepository : IPrinterModelRepository
{
    public List<PrinterModel> Items { get; } = new();

    public Task<PrinterModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(m => m.Id == id));

    public Task<List<PrinterModel>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(Items.ToList());

    public Task AddAsync(PrinterModel entity, CancellationToken cancellationToken = default)
    {
        Items.Add(entity);
        return Task.CompletedTask;
    }
}

internal sealed class FakePrinterBrandRepository : IPrinterBrandRepository
{
    public List<PrinterBrand> Items { get; } = new();

    public Task<PrinterBrand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(b => b.Id == id));

    public Task<List<PrinterBrand>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(Items.ToList());

    public Task AddAsync(PrinterBrand entity, CancellationToken cancellationToken = default)
    {
        Items.Add(entity);
        return Task.CompletedTask;
    }
}

internal sealed class FakeFilamentBrandRepository : IFilamentBrandRepository
{
    public List<FilamentBrand> Items { get; } = new();

    public Task<FilamentBrand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(b => b.Id == id));

    public Task<List<FilamentBrand>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(Items.ToList());

    public Task AddAsync(FilamentBrand filamentBrand, CancellationToken cancellationToken = default)
    {
        Items.Add(filamentBrand);
        return Task.CompletedTask;
    }
}

internal sealed class FakeFilamentTypeRepository : IFilamentTypeRepository
{
    public List<FilamentType> Items { get; } = new();

    public Task<FilamentType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(t => t.Id == id));

    public Task<List<FilamentType>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(Items.ToList());

    public Task AddAsync(FilamentType filamentType, CancellationToken cancellationToken = default)
    {
        Items.Add(filamentType);
        return Task.CompletedTask;
    }
}

internal sealed class FakeUserSettingsRepository : IUserSettingsRepository
{
    public List<UserSettings> Items { get; } = new();

    public Task<UserSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(s => s.UserId == userId));

    public Task<UserSettings> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var settings = Items.FirstOrDefault(s => s.UserId == userId);

        if (settings is null)
        {
            settings = UserSettings.CreateDefault(userId);
            Items.Add(settings);
        }

        return Task.FromResult(settings);
    }

    public Task AddAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        Items.Add(settings);
        return Task.CompletedTask;
    }
}

/// <summary>Plano que libera tudo: para testes que nao tratam de regras de plano.</summary>
internal sealed class AllowAllEntitlementService : IEntitlementService
{
    public Task EnsureWithinLimitAsync(Guid userId, PlanEntitlementKey key, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task EnsureFeatureAsync(Guid userId, PlanEntitlementKey key, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<bool> IsFeatureEnabledAsync(Guid userId, PlanEntitlementKey key, CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task<MyPlanDto> GetMyPlanAsync(Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<ProductHistoryWindow?> GetProductHistoryWindowAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<ProductHistoryWindow?>(null);
}

/// <summary>
/// Monta usuarios, plano e repositorios com o EntitlementService real (sem banco) para os testes de regras de plano.
/// </summary>
internal sealed class PlanTestContext
{
    public FakeUserRepository Users { get; } = new();
    public FakePlanRepository Plans { get; } = new();
    public FakePrinterRepository Printers { get; } = new();
    public FakeFilamentRepository Filaments { get; } = new();
    public FakeProductRepository Products { get; } = new();
    public FakePrinterModelRepository PrinterModels { get; } = new();
    public FakePrinterBrandRepository PrinterBrands { get; } = new();
    public FakeFilamentBrandRepository FilamentBrands { get; } = new();
    public FakeFilamentTypeRepository FilamentTypes { get; } = new();
    public FakeUserSettingsRepository Settings { get; } = new();
    public FakeTimeProvider Time { get; } = new();
    public EntitlementService Entitlements { get; }

    public PlanTestContext()
    {
        Plans.Users = Users;
        Entitlements = new EntitlementService(Users, Plans, Printers, Filaments, Products, Time);
    }

    public Plan AddPlan(params (PlanEntitlementKey Key, int? IntValue, bool? BoolValue)[] entitlements)
    {
        var plan = TestPlans.Create("Basico", isDefault: Plans.Items.Count == 0, entitlements: entitlements);
        Plans.Items.Add(plan);
        return plan;
    }

    public User AddUser(Plan plan, bool admin = false)
    {
        var user = new User("Fulano", $"{Guid.NewGuid():N}@b.com", "hash", Guid.NewGuid(), plan.Id);

        if (admin)
            user.MarkAsAdmin();

        Users.Items.Add(user);
        return user;
    }

    public PrinterModel AddPrinterModel()
    {
        var brand = new PrinterBrand("Bambu");
        var model = new PrinterModel("A1", brand.Id, null);
        PrinterBrands.Items.Add(brand);
        PrinterModels.Items.Add(model);
        return model;
    }

    public Printer AddPrinter(Guid userId, Guid? modelId = null, bool depreciation = false, bool maintenance = false, bool failure = false)
    {
        var printer = new Printer(modelId ?? Guid.NewGuid(), null, 100m, 30m, 1000m, 1000m, 5m, 5m, depreciation, maintenance, failure, userId);
        Printers.Items.Add(printer);
        return printer;
    }

    public Product AddProduct(Guid userId, DateTime createdAtUtc)
    {
        var product = new Product("Peca", null, 1m, 1, 1, Guid.NewGuid(), userId).WithCreatedAt(createdAtUtc);
        Products.Items.Add(product);
        return product;
    }

    public PrinterService PrinterService() =>
        new(Printers, PrinterModels, PrinterBrands, Entitlements, new FakeUnitOfWork());

    public FilamentService FilamentService() =>
        new(Filaments, FilamentBrands, FilamentTypes, Entitlements, new FakeUnitOfWork());

    public ProductService ProductService() =>
        new(Products, Filaments, FilamentBrands, Settings, Printers, PrinterModels, PrinterBrands, Entitlements, new FakeUnitOfWork());

    /// <summary>Instante UTC de um horario local de Sao Paulo (UTC-3).</summary>
    public static DateTime SaoPaulo(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc).AddHours(3);
}
