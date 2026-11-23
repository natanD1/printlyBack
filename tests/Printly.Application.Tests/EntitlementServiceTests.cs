using Printly.Application.Services;
using Printly.Domain.Enums;
using Printly.Domain.Exceptions;
using static Printly.Application.Tests.TestPlans;

namespace Printly.Application.Tests;

public class EntitlementServiceTests
{
    [Fact]
    public async Task Limit_Reached_ThrowsPlanLimitReached_WithPlanName()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxPrinters, 1)));
        ctx.AddPrinter(user.Id);

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(
            () => ctx.Entitlements.EnsureWithinLimitAsync(user.Id, PlanEntitlementKey.MaxPrinters));

        Assert.Equal(PlanRestrictionException.PlanLimitReached, ex.Code);
        Assert.Equal("Seu plano Basico permite ate 1 impressora. Faca upgrade para cadastrar mais.", ex.Message);
    }

    [Fact]
    public async Task Limit_BelowLimit_Passes()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxPrinters, 2)));
        ctx.AddPrinter(user.Id);

        await ctx.Entitlements.EnsureWithinLimitAsync(user.Id, PlanEntitlementKey.MaxPrinters);
    }

    [Fact]
    public async Task Limit_Null_IsUnlimited()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxFilaments, null)));

        for (var i = 0; i < 50; i++)
            ctx.Filaments.Items.Add(new Domain.Entities.Filament("PLA", Guid.NewGuid(), Guid.NewGuid(), "#000000", 1m, 100m, user.Id));

        await ctx.Entitlements.EnsureWithinLimitAsync(user.Id, PlanEntitlementKey.MaxFilaments);
    }

    [Fact]
    public async Task Limit_Zero_BlocksFirstItem()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxPrinters, 0)));

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(
            () => ctx.Entitlements.EnsureWithinLimitAsync(user.Id, PlanEntitlementKey.MaxPrinters));

        Assert.Equal(PlanRestrictionException.PlanLimitReached, ex.Code);
        Assert.Equal("Seu plano Basico nao permite cadastrar impressoras. Faca upgrade para liberar.", ex.Message);
    }

    [Fact]
    public async Task Feature_Off_ThrowsPlanFeatureUnavailable()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Feature(PlanEntitlementKey.CompanyLogoInQuote, false)));

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(
            () => ctx.Entitlements.EnsureFeatureAsync(user.Id, PlanEntitlementKey.CompanyLogoInQuote));

        Assert.Equal(PlanRestrictionException.PlanFeatureUnavailable, ex.Code);
        Assert.Equal("Seu plano Basico nao permite usar o logo da empresa no orcamento. Faca upgrade para liberar.", ex.Message);
        Assert.False(await ctx.Entitlements.IsFeatureEnabledAsync(user.Id, PlanEntitlementKey.CompanyLogoInQuote));
    }

    [Fact]
    public async Task Feature_On_Passes()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Feature(PlanEntitlementKey.CompanyLogoInQuote, true)));

        await ctx.Entitlements.EnsureFeatureAsync(user.Id, PlanEntitlementKey.CompanyLogoInQuote);
        Assert.True(await ctx.Entitlements.IsFeatureEnabledAsync(user.Id, PlanEntitlementKey.CompanyLogoInQuote));
    }

    [Fact]
    public async Task MissingKey_LimitIsUnlimited_FeatureIsOff()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan());

        for (var i = 0; i < 10; i++)
            ctx.AddPrinter(user.Id);

        await ctx.Entitlements.EnsureWithinLimitAsync(user.Id, PlanEntitlementKey.MaxPrinters);
        await Assert.ThrowsAsync<PlanRestrictionException>(
            () => ctx.Entitlements.EnsureFeatureAsync(user.Id, PlanEntitlementKey.OptionalCostsEnabled));
        Assert.Null(await ctx.Entitlements.GetProductHistoryWindowAsync(user.Id));
    }

    [Fact]
    public async Task Admin_PassesEverything()
    {
        var ctx = new PlanTestContext();
        var plan = ctx.AddPlan(
            Limit(PlanEntitlementKey.MaxPrinters, 0),
            Limit(PlanEntitlementKey.ProductHistoryMonths, 0),
            Feature(PlanEntitlementKey.CompanyLogoInQuote, false));
        var admin = ctx.AddUser(plan, admin: true);
        ctx.AddPrinter(admin.Id);

        await ctx.Entitlements.EnsureWithinLimitAsync(admin.Id, PlanEntitlementKey.MaxPrinters);
        await ctx.Entitlements.EnsureFeatureAsync(admin.Id, PlanEntitlementKey.CompanyLogoInQuote);
        await ctx.Entitlements.EnsureFeatureAsync(admin.Id, PlanEntitlementKey.RemoveBrandingFooter);
        Assert.Null(await ctx.Entitlements.GetProductHistoryWindowAsync(admin.Id));
    }

    [Fact]
    public async Task Counts_OnlyOwnUser()
    {
        var ctx = new PlanTestContext();
        var plan = ctx.AddPlan(Limit(PlanEntitlementKey.MaxPrinters, 1), Limit(PlanEntitlementKey.MaxProductsPerMonth, 1));
        var user = ctx.AddUser(plan);
        var other = ctx.AddUser(plan);
        ctx.AddPrinter(other.Id);
        ctx.AddPrinter(other.Id);
        ctx.AddProduct(other.Id, ctx.Time.UtcNow.UtcDateTime);

        await ctx.Entitlements.EnsureWithinLimitAsync(user.Id, PlanEntitlementKey.MaxPrinters);
        await ctx.Entitlements.EnsureWithinLimitAsync(user.Id, PlanEntitlementKey.MaxProductsPerMonth);

        var myPlan = await ctx.Entitlements.GetMyPlanAsync(user.Id);
        Assert.Equal(0, myPlan.Entitlements.Single(e => e.Key == "MaxPrinters").Used);
    }

    [Fact]
    public async Task ProductsPerMonth_LastInstantOfMonth_CountsProductsOfThatMonth()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxProductsPerMonth, 1)));
        // 31/10 23:59:59 em Sao Paulo = 01/11 02:59:59 UTC: ainda e outubro pro usuario.
        ctx.Time.UtcNow = new DateTimeOffset(PlanTestContext.SaoPaulo(2026, 10, 31, 23, 59, 59));
        ctx.AddProduct(user.Id, PlanTestContext.SaoPaulo(2026, 10, 1, 0, 0, 0));

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(
            () => ctx.Entitlements.EnsureWithinLimitAsync(user.Id, PlanEntitlementKey.MaxProductsPerMonth));

        Assert.Equal("Seu plano Basico permite ate 1 produto por mes. Faca upgrade para cadastrar mais.", ex.Message);
    }

    [Fact]
    public async Task ProductsPerMonth_FirstInstantOfMonth_StartsFresh()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxProductsPerMonth, 1)));
        // Produto criado no ultimo instante de outubro (Sao Paulo), ja em novembro no UTC.
        ctx.AddProduct(user.Id, PlanTestContext.SaoPaulo(2026, 10, 31, 23, 59, 59));
        ctx.Time.UtcNow = new DateTimeOffset(PlanTestContext.SaoPaulo(2026, 11, 1, 0, 0, 0));

        await ctx.Entitlements.EnsureWithinLimitAsync(user.Id, PlanEntitlementKey.MaxProductsPerMonth);

        // Produto do primeiro instante de novembro conta no mes novo.
        ctx.AddProduct(user.Id, PlanTestContext.SaoPaulo(2026, 11, 1, 0, 0, 0));
        await Assert.ThrowsAsync<PlanRestrictionException>(
            () => ctx.Entitlements.EnsureWithinLimitAsync(user.Id, PlanEntitlementKey.MaxProductsPerMonth));
    }

    [Fact]
    public void MonthStartUtc_UsesSaoPauloCalendar()
    {
        // 01/11 01:00 UTC ainda e 31/10 22:00 em Sao Paulo.
        var now = new DateTimeOffset(2026, 11, 1, 1, 0, 0, TimeSpan.Zero);

        Assert.Equal(new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc), BrazilTime.MonthStartUtc(now));
        Assert.Equal(new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc), BrazilTime.MonthStartUtc(now, 1));
        Assert.Equal(new DateTime(2026, 7, 1, 3, 0, 0, DateTimeKind.Utc), BrazilTime.MonthStartUtc(now, -3));
    }

    [Fact]
    public async Task MyPlan_HasOneEntryPerCatalogItem_WithLabelUsageAndFeatures()
    {
        var ctx = new PlanTestContext();
        var plan = ctx.AddPlan(
            Limit(PlanEntitlementKey.MaxPrinters, 1),
            Limit(PlanEntitlementKey.ProductHistoryMonths, 3),
            Feature(PlanEntitlementKey.CompanyLogoInQuote, true));
        var user = ctx.AddUser(plan);
        ctx.AddPrinter(user.Id);

        var myPlan = await ctx.Entitlements.GetMyPlanAsync(user.Id);

        Assert.Equal(plan.Id, myPlan.PlanId);
        Assert.Equal("Basico", myPlan.PlanName);
        Assert.False(myPlan.IsAdmin);
        Assert.Equal(PlanEntitlementCatalog.All.Select(d => d.Key.ToString()), myPlan.Entitlements.Select(e => e.Key));

        var printers = myPlan.Entitlements.Single(e => e.Key == "MaxPrinters");
        Assert.Equal(("Impressoras", "limit", 1, 1, (bool?)null), (printers.Label, printers.Type, printers.Limit, printers.Used, printers.Enabled));

        var history = myPlan.Entitlements.Single(e => e.Key == "ProductHistoryMonths");
        Assert.Equal(3, history.Limit);
        Assert.Null(history.Used);

        var filaments = myPlan.Entitlements.Single(e => e.Key == "MaxFilaments");
        Assert.Null(filaments.Limit);
        Assert.Equal(0, filaments.Used);

        var logo = myPlan.Entitlements.Single(e => e.Key == "CompanyLogoInQuote");
        Assert.Equal(("feature", (int?)null, (int?)null, (bool?)true), (logo.Type, logo.Limit, logo.Used, logo.Enabled));
        Assert.False(myPlan.Entitlements.Single(e => e.Key == "RemoveBrandingFooter").Enabled);
    }

    [Fact]
    public async Task MyPlan_Admin_LimitsNullAndFeaturesOn()
    {
        var ctx = new PlanTestContext();
        var admin = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxPrinters, 1)), admin: true);

        var myPlan = await ctx.Entitlements.GetMyPlanAsync(admin.Id);

        Assert.True(myPlan.IsAdmin);
        Assert.All(myPlan.Entitlements.Where(e => e.Type == "limit"), e => Assert.Null(e.Limit));
        Assert.All(myPlan.Entitlements.Where(e => e.Type == "feature"), e => Assert.True(e.Enabled));
    }

    [Fact]
    public void Catalog_CoversEveryKey_WithUniqueDefinitions()
    {
        Assert.Equal(Enum.GetValues<PlanEntitlementKey>().OrderBy(k => k), PlanEntitlementCatalog.All.Select(d => d.Key).OrderBy(k => k));
        Assert.All(PlanEntitlementCatalog.All.Where(d => d.Type == PlanEntitlementType.Limit), d =>
        {
            Assert.False(string.IsNullOrWhiteSpace(d.MessageUnitSingular));
            Assert.False(string.IsNullOrWhiteSpace(d.MessageUnitPlural));
        });
        Assert.All(PlanEntitlementCatalog.All.Where(d => d.Type == PlanEntitlementType.Feature), d =>
            Assert.False(string.IsNullOrWhiteSpace(d.MessageFeature)));
    }
}
