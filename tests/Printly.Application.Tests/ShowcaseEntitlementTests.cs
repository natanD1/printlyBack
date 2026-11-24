using Printly.Application.Services;
using Printly.Domain.Enums;
using Printly.Domain.Exceptions;
using static Printly.Application.Tests.TestPlans;

namespace Printly.Application.Tests;

/// <summary>EnsureWithinLimitAsync com <c>additional</c>, contador e catalogo das regras da vitrine.</summary>
public class ShowcaseEntitlementTests
{
    [Fact]
    public async Task Additional_PassesWhenEverythingFits_BlocksWhenOneOver()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxPrinters, 3)));
        ctx.AddPrinter(user.Id);

        await ctx.Entitlements.EnsureWithinLimitAsync(user.Id, PlanEntitlementKey.MaxPrinters, 2);

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(
            () => ctx.Entitlements.EnsureWithinLimitAsync(user.Id, PlanEntitlementKey.MaxPrinters, 3));

        Assert.Equal(PlanRestrictionException.PlanLimitReached, ex.Code);
        Assert.Equal("Seu plano Basico permite ate 3 impressoras. Ainda cabem 2. Faca upgrade para cadastrar mais.", ex.Message);
    }

    [Fact]
    public async Task Additional_ZeroOrNegative_NeverBlocks()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxPrinters, 0)));

        await ctx.Entitlements.EnsureWithinLimitAsync(user.Id, PlanEntitlementKey.MaxPrinters, 0);
        await ctx.Entitlements.EnsureWithinLimitAsync(user.Id, PlanEntitlementKey.MaxPrinters, -1);
    }

    [Fact]
    public async Task Additional_UsageAboveLimitAfterDowngrade_SaysNoneFit()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxPrinters, 1)));
        ctx.AddPrinter(user.Id);
        ctx.AddPrinter(user.Id);

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(
            () => ctx.Entitlements.EnsureWithinLimitAsync(user.Id, PlanEntitlementKey.MaxPrinters, 2));

        Assert.Contains("Ainda cabem 0.", ex.Message);
    }

    [Fact]
    public async Task Additional_Admin_IsUnlimited()
    {
        var ctx = new PlanTestContext();
        var admin = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxShowcaseItems, 0)), admin: true);

        await ctx.Entitlements.EnsureWithinLimitAsync(admin.Id, PlanEntitlementKey.MaxShowcaseItems, 500);
        Assert.Null(await ctx.Entitlements.GetLimitAsync(admin.Id, PlanEntitlementKey.MaxShowcaseItems));
    }

    [Fact]
    public async Task GetLimit_ReturnsPlanValue_NullWhenMissing()
    {
        var ctx = new PlanTestContext();
        var user = ctx.AddUser(ctx.AddPlan(Limit(PlanEntitlementKey.MaxShowcaseItems, 7)));
        var other = ctx.AddUser(ctx.AddPlan());

        Assert.Equal(7, await ctx.Entitlements.GetLimitAsync(user.Id, PlanEntitlementKey.MaxShowcaseItems));
        Assert.Null(await ctx.Entitlements.GetLimitAsync(other.Id, PlanEntitlementKey.MaxShowcaseItems));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ctx.Entitlements.GetLimitAsync(user.Id, PlanEntitlementKey.ShowcaseEnabled));
    }

    [Fact]
    public async Task MyPlan_ListsShowcaseRules_WithUsage()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(limit: 5);
        var hidden = ctx.AddProduct(user.Id, "Oculto");
        var showcase = ctx.AddShowcase(user.Id, "minha-loja", ctx.AddProduct(user.Id), ctx.AddProduct(user.Id, "B"), hidden);
        showcase.GetItem(hidden.Id)!.SetVisible(false);

        var myPlan = await ctx.Plan.Entitlements.GetMyPlanAsync(user.Id);

        var enabled = myPlan.Entitlements.Single(e => e.Key == "ShowcaseEnabled");
        var items = myPlan.Entitlements.Single(e => e.Key == "MaxShowcaseItems");
        Assert.True(enabled.Enabled);
        Assert.Equal(5, items.Limit);
        Assert.Equal(2, items.Used);
    }

    [Fact]
    public void Catalog_HasShowcaseGroup_AndRenamedLabels()
    {
        var enabled = PlanEntitlementCatalog.Get(PlanEntitlementKey.ShowcaseEnabled);
        var items = PlanEntitlementCatalog.Get(PlanEntitlementKey.MaxShowcaseItems);

        Assert.Equal(("Vitrine", "Vitrine pública", PlanEntitlementType.Feature), (enabled.Group, enabled.Label, enabled.Type));
        Assert.Equal("usar a vitrine", enabled.MessageFeature);
        Assert.Equal(("Vitrine", "Produtos na vitrine", PlanEntitlementType.Limit), (items.Group, items.Label, items.Type));
        Assert.Equal(("item na vitrine", "itens na vitrine"), (items.UnitSingular, items.UnitPlural));
        Assert.Equal(("item na vitrine", "itens na vitrine"), (items.MessageUnitSingular, items.MessageUnitPlural));
        Assert.Equal("Logo da empresa (orçamento e vitrine)", PlanEntitlementCatalog.Get(PlanEntitlementKey.CompanyLogoInQuote).Label);
        Assert.Equal("Remover marca Printly (orçamento e vitrine)", PlanEntitlementCatalog.Get(PlanEntitlementKey.RemoveBrandingFooter).Label);
    }

    [Fact]
    public void Enum_NewKeysAppendedAtEnd()
    {
        var values = Enum.GetValues<PlanEntitlementKey>();

        Assert.Equal(PlanEntitlementKey.ShowcaseEnabled, values[^2]);
        Assert.Equal(PlanEntitlementKey.MaxShowcaseItems, values[^1]);
        Assert.Equal(7, (int)PlanEntitlementKey.ShowcaseEnabled);
    }
}
