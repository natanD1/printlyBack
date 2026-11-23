using Printly.Application.DTOs.CompanyProfile;
using Printly.Application.DTOs.Filaments;
using Printly.Application.DTOs.Printers;
using Printly.Application.DTOs.Products;
using Printly.Application.Services;
using Printly.Domain.Entities;
using Printly.Domain.Enums;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;
using static Printly.Application.Tests.TestPlans;

namespace Printly.Application.Tests;

/// <summary>Regras de plano aplicadas nos servicos (impressora, filamento, produto, empresa).</summary>
public class PlanRulesInServicesTests
{
    private static SavePrinterRequest PrinterRequest(
        Guid modelId, string? nickname = null, bool depreciation = false, bool maintenance = false, bool failure = false) =>
        new(modelId, nickname, 100m, 30m, 1000m, 1000m, 5m, 5m, depreciation, maintenance, failure);

    // ---------- Impressoras ----------

    [Fact]
    public async Task Printer_Create_BlockedWhenMaxPrintersReached()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxPrinters, 1)));
        var model = ctx.AddPrinterModel();
        ctx.AddPrinter(user.Id, model.Id);

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(
            () => ctx.PrinterService().CreateAsync(PrinterRequest(model.Id), user.Id));

        Assert.Equal(PlanRestrictionException.PlanLimitReached, ex.Code);
        Assert.Single(ctx.Printers.Items);
    }

    [Fact]
    public async Task Printer_Create_AllowedBelowLimit()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxPrinters, 2)));
        var model = ctx.AddPrinterModel();
        ctx.AddPrinter(user.Id, model.Id);

        await ctx.PrinterService().CreateAsync(PrinterRequest(model.Id), user.Id);

        Assert.Equal(2, ctx.Printers.Items.Count);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task Printer_Create_BlocksTurningOnOptionalCosts_WhenFeatureOff(bool depreciation, bool maintenance, bool failure)
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Feature(PlanEntitlementKey.OptionalCostsEnabled, false)));
        var model = ctx.AddPrinterModel();

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(
            () => ctx.PrinterService().CreateAsync(PrinterRequest(model.Id, depreciation: depreciation, maintenance: maintenance, failure: failure), user.Id));

        Assert.Equal(PlanRestrictionException.PlanFeatureUnavailable, ex.Code);
        Assert.Empty(ctx.Printers.Items);
    }

    [Fact]
    public async Task Printer_Create_WithoutOptionalCosts_AllowedWhenFeatureOff()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Feature(PlanEntitlementKey.OptionalCostsEnabled, false)));
        var model = ctx.AddPrinterModel();

        await ctx.PrinterService().CreateAsync(PrinterRequest(model.Id), user.Id);

        Assert.Single(ctx.Printers.Items);
    }

    [Fact]
    public async Task Printer_Update_KeepingOptionalCostOn_AndEditingOtherFields_IsAllowed()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Feature(PlanEntitlementKey.OptionalCostsEnabled, false)));
        var model = ctx.AddPrinterModel();
        var printer = ctx.AddPrinter(user.Id, model.Id, depreciation: true);

        var dto = await ctx.PrinterService().UpdateAsync(printer.Id, PrinterRequest(model.Id, nickname: "Nova", depreciation: true), user.Id);

        Assert.Equal("Nova", dto.Nickname);
        Assert.True(dto.DepreciationEnabled);
    }

    [Fact]
    public async Task Printer_Update_TurningOnAnotherOptionalCost_IsBlocked()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Feature(PlanEntitlementKey.OptionalCostsEnabled, false)));
        var model = ctx.AddPrinterModel();
        var printer = ctx.AddPrinter(user.Id, model.Id, depreciation: true);

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(
            () => ctx.PrinterService().UpdateAsync(printer.Id, PrinterRequest(model.Id, depreciation: true, maintenance: true), user.Id));

        Assert.Equal(PlanRestrictionException.PlanFeatureUnavailable, ex.Code);
        Assert.False(printer.MaintenanceEnabled);
    }

    [Fact]
    public async Task Printer_Update_TurningOffOptionalCost_IsAllowed()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Feature(PlanEntitlementKey.OptionalCostsEnabled, false)));
        var model = ctx.AddPrinterModel();
        var printer = ctx.AddPrinter(user.Id, model.Id, depreciation: true, maintenance: true);

        var dto = await ctx.PrinterService().UpdateAsync(printer.Id, PrinterRequest(model.Id, maintenance: true), user.Id);

        Assert.False(dto.DepreciationEnabled);
        Assert.True(dto.MaintenanceEnabled);
    }

    [Fact]
    public async Task Printer_Update_TurningOnOptionalCost_AllowedWhenFeatureOn()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Feature(PlanEntitlementKey.OptionalCostsEnabled, true)));
        var model = ctx.AddPrinterModel();
        var printer = ctx.AddPrinter(user.Id, model.Id);

        var dto = await ctx.PrinterService().UpdateAsync(printer.Id, PrinterRequest(model.Id, failure: true), user.Id);

        Assert.True(dto.FailureEnabled);
    }

    // ---------- Filamentos ----------

    [Fact]
    public async Task Filament_Create_BlockedWhenMaxFilamentsReached()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxFilaments, 1)));
        ctx.Filaments.Items.Add(new Filament("PLA", Guid.NewGuid(), Guid.NewGuid(), "#000000", 1m, 100m, user.Id));

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(() => ctx.FilamentService().CreateAsync(
            new CreateFilamentRequest("PETG", Guid.NewGuid(), Guid.NewGuid(), "#FFFFFF", 1m, 100m), user.Id));

        Assert.Equal("Seu plano Basico permite ate 1 filamento. Faca upgrade para cadastrar mais.", ex.Message);
        Assert.Single(ctx.Filaments.Items);
    }

    [Fact]
    public async Task Filament_Create_AllowedBelowLimit()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxFilaments, 1)));
        var brand = new FilamentBrand("Voolt", null);
        var type = new FilamentType("PLA");
        ctx.FilamentBrands.Items.Add(brand);
        ctx.FilamentTypes.Items.Add(type);

        await ctx.FilamentService().CreateAsync(new CreateFilamentRequest("PLA", brand.Id, type.Id, "#FFFFFF", 1m, 100m), user.Id);

        Assert.Single(ctx.Filaments.Items);
    }

    // ---------- Produtos por mes ----------

    [Fact]
    public async Task Product_Create_BlockedWhenMonthlyLimitReached()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxProductsPerMonth, 1)));
        var printer = ctx.AddPrinter(user.Id);
        ctx.AddProduct(user.Id, ctx.Time.UtcNow.UtcDateTime);

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(() => ctx.ProductService().CreateAsync(
            new CreateProductRequest("Vaso", null, 1m, 1, 1, printer.Id, null, new List<ProductFilamentRequest>()), user.Id));

        Assert.Equal(PlanRestrictionException.PlanLimitReached, ex.Code);
        Assert.Single(ctx.Products.Items);
    }

    [Fact]
    public async Task Product_Create_ProductsFromPreviousMonthDoNotCount()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxProductsPerMonth, 1)));
        var printer = ctx.AddPrinter(user.Id);
        ctx.Time.UtcNow = new DateTimeOffset(PlanTestContext.SaoPaulo(2026, 11, 10, 12));
        ctx.AddProduct(user.Id, PlanTestContext.SaoPaulo(2026, 10, 31, 23, 59, 59));

        await ctx.ProductService().CreateAsync(
            new CreateProductRequest("Vaso", null, 1m, 1, 1, printer.Id, null, new List<ProductFilamentRequest>()), user.Id);

        Assert.Equal(2, ctx.Products.Items.Count);
    }

    // ---------- Historico de produtos ----------

    /// <summary>Agora = 23/11/2026 09:00 em Sao Paulo; com 3 meses, o inicio e 01/08/2026 00:00 SP (03:00 UTC).</summary>
    private static (PlanTestContext Ctx, User User, ProductService Service) HistoryContext(int? months, bool admin = false)
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.ProductHistoryMonths, months)), admin);
        ctx.Time.UtcNow = new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);
        return (ctx, user, ctx.ProductService());
    }

    private static readonly DateTime AugustStartUtc = new(2026, 8, 1, 3, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task History_FromInsideLimit_IsAllowed()
    {
        var (ctx, user, service) = HistoryContext(3);
        var from = new DateTime(2026, 9, 1, 3, 0, 0, DateTimeKind.Utc);

        await service.GetAllAsync(user.Id, from, null);

        Assert.Equal(from, ctx.Products.LastQuery!.Value.From);
    }

    [Fact]
    public async Task History_FromBeforeLimit_ThrowsPlanLimitReached()
    {
        var (_, user, service) = HistoryContext(3);

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(
            () => service.GetAllAsync(user.Id, new DateTime(2026, 7, 1, 3, 0, 0, DateTimeKind.Utc), null));

        Assert.Equal(PlanRestrictionException.PlanLimitReached, ex.Code);
        Assert.Equal("Seu plano permite consultar ate 3 meses anteriores.", ex.Message);
    }

    [Fact]
    public async Task History_FromMissing_UsesAllowedStart()
    {
        var (ctx, user, service) = HistoryContext(3);
        ctx.AddProduct(user.Id, AugustStartUtc.AddDays(-1));
        ctx.AddProduct(user.Id, AugustStartUtc);

        var products = await service.GetAllAsync(user.Id, null, null);

        Assert.Equal(AugustStartUtc, ctx.Products.LastQuery!.Value.From);
        Assert.Single(products);
    }

    [Fact]
    public async Task History_ToOnlyBeforeAllowedStart_IsBlocked()
    {
        var (_, user, service) = HistoryContext(3);

        await Assert.ThrowsAsync<PlanRestrictionException>(() => service.GetAllAsync(user.Id, null, AugustStartUtc));
    }

    [Fact]
    public async Task History_ZeroMonths_OnlyCurrentMonth()
    {
        var (ctx, user, service) = HistoryContext(0);
        var novemberStartUtc = new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc);

        await service.GetAllAsync(user.Id, null, null);
        Assert.Equal(novemberStartUtc, ctx.Products.LastQuery!.Value.From);

        await service.GetAllAsync(user.Id, novemberStartUtc, null);

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(
            () => service.GetAllAsync(user.Id, new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc), null));
        Assert.Equal("Seu plano permite consultar apenas o mes atual.", ex.Message);
    }

    [Fact]
    public async Task History_MonthTurn_ClientBehindSaoPaulo_StillSeesPreviousMonth()
    {
        // 01/12/2026 00:30 em Sao Paulo = 30/11 22:30 no Acre: o navegador ainda pede novembro.
        var (ctx, user, service) = HistoryContext(0);
        ctx.Time.UtcNow = new DateTimeOffset(2026, 12, 1, 3, 30, 0, TimeSpan.Zero);
        var novemberStartAcreUtc = new DateTime(2026, 11, 1, 5, 0, 0, DateTimeKind.Utc);

        await service.GetAllAsync(user.Id, novemberStartAcreUtc, null);
        Assert.Equal(novemberStartAcreUtc, ctx.Products.LastQuery!.Value.From);

        // Passadas as 2h de folga, o mes de referencia e dezembro.
        ctx.Time.UtcNow = new DateTimeOffset(2026, 12, 1, 5, 30, 0, TimeSpan.Zero);
        await Assert.ThrowsAsync<PlanRestrictionException>(
            () => service.GetAllAsync(user.Id, novemberStartAcreUtc, null));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(0, true)]
    public async Task History_UnlimitedOrAdmin_HasNoRestriction(int? months, bool admin)
    {
        var (ctx, user, service) = HistoryContext(months, admin);
        var veryOld = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await service.GetAllAsync(user.Id, null, null);
        Assert.Null(ctx.Products.LastQuery!.Value.From);

        await service.GetAllAsync(user.Id, veryOld, null);
        Assert.Equal(veryOld, ctx.Products.LastQuery!.Value.From);
    }

    [Fact]
    public async Task History_OneDayTolerance()
    {
        var (ctx, user, service) = HistoryContext(3);

        // Cliente em outro fuso pode mandar o inicio do mes algumas horas antes: ate 1 dia passa.
        await service.GetAllAsync(user.Id, AugustStartUtc.AddDays(-1), null);
        Assert.Equal(AugustStartUtc.AddDays(-1), ctx.Products.LastQuery!.Value.From);

        await Assert.ThrowsAsync<PlanRestrictionException>(
            () => service.GetAllAsync(user.Id, AugustStartUtc.AddDays(-1).AddSeconds(-1), null));
    }

    // ---------- Logo da empresa ----------

    private static readonly string PngLogo = Convert.ToBase64String(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

    private static SaveCompanyProfileRequest CompanyRequest(string name = "Empresa", string? logo = null, bool removeLogo = false) =>
        new(name, logo, removeLogo, null, null, null, null, null);

    private static (CompanyProfileService Service, CompanyRepository Repository, User User) CompanyContext(bool logoEnabled)
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Feature(PlanEntitlementKey.CompanyLogoInQuote, logoEnabled)));
        var repository = new CompanyRepository();
        return (new CompanyProfileService(repository, ctx.Entitlements, new FakeUnitOfWork()), repository, user);
    }

    [Fact]
    public async Task Logo_SendingLogo_BlockedWhenFeatureOff()
    {
        var (service, repository, user) = CompanyContext(logoEnabled: false);

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(() => service.SaveAsync(user.Id, CompanyRequest(logo: PngLogo)));

        Assert.Equal(PlanRestrictionException.PlanFeatureUnavailable, ex.Code);
        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task Logo_TextsOnlyAndRemoveLogo_AllowedWhenFeatureOff()
    {
        var (service, _, user) = CompanyContext(logoEnabled: false);

        var created = await service.SaveAsync(user.Id, CompanyRequest(name: "Loja"));
        var updated = await service.SaveAsync(user.Id, CompanyRequest(name: "Loja 2", removeLogo: true));

        Assert.Equal("Loja", created.Name);
        Assert.Equal("Loja 2", updated.Name);
    }

    [Fact]
    public async Task Logo_ExistingLogoIsKept_WhenSavingTextsWithFeatureOff()
    {
        var (service, repository, user) = CompanyContext(logoEnabled: false);
        // Logo salvo enquanto o plano permitia.
        repository.Items.Add(new CompanyProfile(user.Id, "Loja", PngLogo, null, null, null, null, null));

        var updated = await service.SaveAsync(user.Id, CompanyRequest(name: "Loja nova"));

        Assert.Equal("Loja nova", updated.Name);
        Assert.Equal(PngLogo, updated.LogoBase64);
    }

    [Fact]
    public async Task Logo_SendingLogo_AllowedWhenFeatureOn()
    {
        var (service, _, user) = CompanyContext(logoEnabled: true);

        var dto = await service.SaveAsync(user.Id, CompanyRequest(logo: PngLogo));

        Assert.Equal(PngLogo, dto.LogoBase64);
    }

    private sealed class CompanyRepository : ICompanyProfileRepository
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
}
