using System.Text.Json;
using Printly.Application.DTOs.CompanyProfile;
using Printly.Application.DTOs.Showcases;
using Printly.Domain.Entities;
using Printly.Domain.Enums;
using Printly.Domain.Exceptions;
using static Printly.Application.Tests.TestPlans;

namespace Printly.Application.Tests;

/// <summary>Vitrine do dono: criacao/slug, itens em lote, limite e recurso do plano, admin.</summary>
public class ShowcaseServiceTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    // ---------- GET / criacao / slug ----------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_WithoutShowcase_SerializesOnlyExistsAndPlanAllows(bool enabled)
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(showcaseEnabled: enabled);

        var dto = await ctx.Service().GetAsync(user.Id);

        Assert.Equal(
            $"{{\"exists\":false,\"planAllows\":{(enabled ? "true" : "false")}}}",
            JsonSerializer.Serialize<object>(dto, WebJson));
    }

    [Fact]
    public async Task Get_WithShowcase_SerializesAllFields_LimitNullWhenUnlimited()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(limit: null);
        ctx.AddShowcase(user.Id, "minha-loja", ctx.AddProduct(user.Id));

        var dto = await ctx.Service().GetAsync(user.Id);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize<object>(dto, WebJson));
        Assert.Equal(
            new[] { "exists", "isBlocked", "isPublished", "items", "limit", "planAllows", "slug", "used" },
            json.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        Assert.True(json.RootElement.GetProperty("exists").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("limit").ValueKind);
        Assert.Equal(
            new[] { "hasPhoto", "isVisible", "name", "outsidePlan", "productId", "productIsProduced", "sortOrder", "unitSalePrice" },
            json.RootElement.GetProperty("items")[0].EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
    }

    [Fact]
    public async Task Create_GeneratesSlugFromCompanyName_AndPublishes()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        ctx.AddCompany(user.Id, "Ateliê São João & Cia!");

        var dto = await ctx.Service().CreateAsync(user.Id);

        Assert.Equal("atelie-sao-joao-cia", dto.Slug);
        Assert.True(dto.IsPublished);
        Assert.False(dto.IsBlocked);
        Assert.Single(ctx.Showcases.Items);
    }

    [Fact]
    public async Task Create_IsIdempotent()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        ctx.AddCompany(user.Id);

        var first = await ctx.Service().CreateAsync(user.Id);
        var second = await ctx.Service().CreateAsync(user.Id);

        Assert.Equal(first.Slug, second.Slug);
        Assert.Single(ctx.Showcases.Items);
    }

    [Fact]
    public async Task Create_WithoutCompany_Throws()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();

        var ex = await Assert.ThrowsAsync<DomainException>(() => ctx.Service().CreateAsync(user.Id));

        Assert.Equal("Cadastre o nome da empresa em Configuracoes antes de criar a vitrine.", ex.Message);
    }

    [Fact]
    public async Task Create_SlugInUse_GetsNumericSuffix()
    {
        var ctx = new ShowcaseTestContext();
        var other = ctx.AddUser();
        ctx.AddShowcase(other.Id, "atelie-3d");
        ctx.AddShowcase(ctx.AddUser().Id, "atelie-3d-2");
        var user = ctx.AddUser();
        ctx.AddCompany(user.Id, "Ateliê 3D");

        var dto = await ctx.Service().CreateAsync(user.Id);

        Assert.Equal("atelie-3d-3", dto.Slug);
    }

    [Theory]
    [InlineData("Loja", "loja-2")]
    [InlineData("!!", "loja-2")]
    [InlineData("Admin", "admin-2")]
    public async Task Create_ReservedOrFallbackName_GetsSuffix(string companyName, string expected)
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        ctx.AddCompany(user.Id, companyName);

        var dto = await ctx.Service().CreateAsync(user.Id);

        Assert.Equal(expected, dto.Slug);
    }

    [Fact]
    public async Task Create_UniqueViolationOnSave_BecomesInUseMessage()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        ctx.AddCompany(user.Id);
        ctx.UnitOfWork.ThrowUniqueViolation = true;

        var ex = await Assert.ThrowsAsync<DomainException>(() => ctx.Service().CreateAsync(user.Id));

        Assert.Equal("Esse endereço já está em uso.", ex.Message);
    }

    // ---------- Slug acompanha o nome da empresa (nao editavel) ----------

    private static SaveCompanyProfileRequest CompanyRequest(string name) =>
        new(name, null, false, null, null, null, null, null);

    [Fact]
    public async Task RenameCompany_RegeneratesShowcaseSlug()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        ctx.AddCompany(user.Id, "Loja Antiga");
        ctx.AddShowcase(user.Id, "loja-antiga");

        await ctx.CompanyService().SaveAsync(user.Id, CompanyRequest("Ateliê Nova Era"));

        Assert.Equal("atelie-nova-era", ctx.Showcases.Items.Single(s => s.UserId == user.Id).Slug);
    }

    [Fact]
    public async Task RenameCompany_SlugTakenByOther_GetsSuffix()
    {
        var ctx = new ShowcaseTestContext();
        ctx.AddShowcase(ctx.AddUser().Id, "nova-era");
        var user = ctx.AddUser();
        ctx.AddCompany(user.Id, "Loja Antiga");
        ctx.AddShowcase(user.Id, "loja-antiga");

        await ctx.CompanyService().SaveAsync(user.Id, CompanyRequest("Nova Era"));

        Assert.Equal("nova-era-2", ctx.Showcases.Items.Single(s => s.UserId == user.Id).Slug);
    }

    [Fact]
    public async Task SaveCompany_SameName_KeepsSuffixedSlug()
    {
        var ctx = new ShowcaseTestContext();
        ctx.AddShowcase(ctx.AddUser().Id, "nova-era");
        var user = ctx.AddUser();
        ctx.AddCompany(user.Id, "Nova Era");
        ctx.AddShowcase(user.Id, "nova-era-2");

        await ctx.CompanyService().SaveAsync(user.Id, CompanyRequest("Nova Era"));

        Assert.Equal("nova-era-2", ctx.Showcases.Items.Single(s => s.UserId == user.Id).Slug);
    }

    [Fact]
    public async Task RenameCompany_ReservedName_SkipsToSuffix()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        ctx.AddCompany(user.Id, "Minha Loja");
        ctx.AddShowcase(user.Id, "minha-loja");

        await ctx.CompanyService().SaveAsync(user.Id, CompanyRequest("Suporte"));

        Assert.Equal("suporte-2", ctx.Showcases.Items.Single(s => s.UserId == user.Id).Slug);
    }

    [Fact]
    public async Task RenameCompany_RaceOnSave_FriendlyMessage()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        ctx.AddCompany(user.Id, "Loja Antiga");
        ctx.AddShowcase(user.Id, "loja-antiga");
        ctx.UnitOfWork.ThrowUniqueViolation = true;

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => ctx.CompanyService().SaveAsync(user.Id, CompanyRequest("Outra Loja")));

        Assert.Contains("endereco da vitrine", ex.Message);
    }

    // ---------- Recurso do plano ----------

    [Fact]
    public async Task FeatureOff_WritesThrowFeatureUnavailable_ReadAndRemoveStillWork()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(showcaseEnabled: false);
        ctx.AddCompany(user.Id);
        var product = ctx.AddProduct(user.Id);
        var other = ctx.AddProduct(user.Id, "Outro");
        ctx.AddShowcase(user.Id, "minha-loja", product, other);
        var service = ctx.Service();

        var writes = new Func<Task>[]
        {
            () => service.CreateAsync(user.Id),
            () => service.SetPublishedAsync(user.Id, false),
            () => service.AddItemsAsync(user.Id, new[] { ctx.AddProduct(user.Id).Id }),
            () => service.ReorderItemsAsync(user.Id, new[] { other.Id, product.Id }),
            () => service.SetItemVisibilityAsync(user.Id, product.Id, false),
        };

        foreach (var write in writes)
        {
            var ex = await Assert.ThrowsAsync<PlanRestrictionException>(write);
            Assert.Equal(PlanRestrictionException.PlanFeatureUnavailable, ex.Code);
        }

        var dto = Assert.IsType<ShowcaseDto>(await service.GetAsync(user.Id));
        Assert.False(dto.PlanAllows);
        Assert.Equal(2, dto.Items.Count);

        await service.RemoveItemAsync(user.Id, product.Id);
        Assert.Single(ctx.Showcases.Items[0].Items);

        Assert.NotEmpty(await service.GetAvailableProductsAsync(user.Id, null));
    }

    // ---------- Itens em lote e limite ----------

    [Fact]
    public async Task AddItems_BulkOverLimit_SavesNothing_AndSaysHowManyFit()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(limit: 3);
        var showcase = ctx.AddShowcase(user.Id, "minha-loja", ctx.AddProduct(user.Id));
        var ids = Enumerable.Range(0, 3).Select(i => ctx.AddProduct(user.Id, $"P{i}").Id).ToList();

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(() => ctx.Service().AddItemsAsync(user.Id, ids));

        Assert.Equal(PlanRestrictionException.PlanLimitReached, ex.Code);
        Assert.Equal("Seu plano Basico permite ate 3 itens na vitrine. Ainda cabem 2. Faca upgrade para cadastrar mais.", ex.Message);
        Assert.Single(showcase.Items);
        Assert.Equal(0, ctx.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddItems_AppendsAtEndInOrder_IgnoresExisting_CountsOnlyNew()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(limit: 2);
        var existing = ctx.AddProduct(user.Id, "A");
        ctx.AddShowcase(user.Id, "minha-loja", existing);
        var added = ctx.AddProduct(user.Id, "B");

        var dto = await ctx.Service().AddItemsAsync(user.Id, new[] { existing.Id, added.Id });

        Assert.Equal(new[] { existing.Id, added.Id }, dto.Items.Select(i => i.ProductId));
        Assert.Equal(new[] { 0, 1 }, dto.Items.Select(i => i.SortOrder));
        Assert.All(dto.Items, i => Assert.True(i.IsVisible));
        Assert.Equal(2, dto.Used);
        Assert.Equal(2, dto.Limit);
        Assert.Equal(1, ctx.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddItems_ConcurrentDuplicate_Becomes400NotGeneric500()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        ctx.AddShowcase(user.Id, "minha-loja");
        var product = ctx.AddProduct(user.Id, "A");
        ctx.UnitOfWork.ThrowUniqueViolation = true;

        var ex = await Assert.ThrowsAsync<DomainException>(() => ctx.Service().AddItemsAsync(user.Id, new[] { product.Id }));

        Assert.Contains("Atualize a pagina", ex.Message);
    }

    [Fact]
    public async Task AddItems_Admin_IsUnlimited()
    {
        var ctx = new ShowcaseTestContext();
        var admin = ctx.AddUser(limit: 1, showcaseEnabled: false, admin: true);
        ctx.AddShowcase(admin.Id, "loja-admin");
        var ids = Enumerable.Range(0, 5).Select(i => ctx.AddProduct(admin.Id, $"P{i}").Id).ToList();

        var dto = await ctx.Service().AddItemsAsync(admin.Id, ids);

        Assert.Equal(5, dto.Items.Count);
        Assert.Null(dto.Limit);
        Assert.All(dto.Items, i => Assert.False(i.OutsidePlan));
    }

    [Fact]
    public async Task AddItems_QuoteProduct_Rejected()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        var showcase = ctx.AddShowcase(user.Id, "minha-loja");
        var quote = ctx.AddProduct(user.Id, produced: false);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => ctx.Service().AddItemsAsync(user.Id, new[] { ctx.AddProduct(user.Id).Id, quote.Id }));

        Assert.Equal("Só produtos produzidos podem ir para a vitrine.", ex.Message);
        Assert.Empty(showcase.Items);
    }

    [Fact]
    public async Task AddItems_ProductOfAnotherUserOrMissing_GenericNotFound()
    {
        var ctx = new ShowcaseTestContext();
        var userA = ctx.AddUser();
        var userB = ctx.AddUser();
        var showcase = ctx.AddShowcase(userA.Id, "loja-a");
        var productOfB = ctx.AddProduct(userB.Id);

        var foreign = await Assert.ThrowsAsync<NotFoundException>(
            () => ctx.Service().AddItemsAsync(userA.Id, new[] { ctx.AddProduct(userA.Id).Id, productOfB.Id }));
        var missing = await Assert.ThrowsAsync<NotFoundException>(
            () => ctx.Service().AddItemsAsync(userA.Id, new[] { Guid.NewGuid() }));

        Assert.Equal("Produto nao encontrado.", foreign.Message);
        Assert.Equal(foreign.Message, missing.Message);
        Assert.Empty(showcase.Items);
    }

    [Fact]
    public async Task AddItems_InvalidLists_Rejected()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        ctx.AddShowcase(user.Id, "minha-loja");
        var id = ctx.AddProduct(user.Id).Id;
        var service = ctx.Service();

        await Assert.ThrowsAsync<DomainException>(() => service.AddItemsAsync(user.Id, Array.Empty<Guid>()));
        await Assert.ThrowsAsync<DomainException>(() => service.AddItemsAsync(user.Id, null));
        await Assert.ThrowsAsync<DomainException>(() => service.AddItemsAsync(user.Id, Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList()));
        await Assert.ThrowsAsync<DomainException>(() => service.AddItemsAsync(user.Id, new[] { id, id }));
    }

    [Fact]
    public async Task AddItems_WithoutShowcase_NotFound()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => ctx.Service().AddItemsAsync(user.Id, new[] { ctx.AddProduct(user.Id).Id }));

        Assert.Equal("Crie a vitrine antes de adicionar produtos.", ex.Message);
    }

    [Fact]
    public async Task ProductBackToQuote_LeavesCounterAndFreesSlot()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(limit: 1);
        var first = ctx.AddProduct(user.Id, "A");
        ctx.AddShowcase(user.Id, "minha-loja", first);

        first.SetProduced(false);

        var dto = await ctx.Service().AddItemsAsync(user.Id, new[] { ctx.AddProduct(user.Id, "B").Id });
        Assert.Equal(1, dto.Used);
        Assert.False(dto.Items.Single(i => i.ProductId == first.Id).ProductIsProduced);
        Assert.All(dto.Items, i => Assert.False(i.OutsidePlan));

        var myPlan = await ctx.Plan.Entitlements.GetMyPlanAsync(user.Id);
        Assert.Equal(1, myPlan.Entitlements.Single(e => e.Key == nameof(PlanEntitlementKey.MaxShowcaseItems)).Used);
    }

    [Fact]
    public async Task SetVisible_RespectsLimit_HidingFreesSlot()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(limit: 1);
        var a = ctx.AddProduct(user.Id, "A");
        var b = ctx.AddProduct(user.Id, "B");
        var showcase = ctx.AddShowcase(user.Id, "minha-loja", a, b);
        showcase.GetItem(b.Id)!.SetVisible(false);
        var service = ctx.Service();

        var ex = await Assert.ThrowsAsync<PlanRestrictionException>(() => service.SetItemVisibilityAsync(user.Id, b.Id, true));
        Assert.Equal(PlanRestrictionException.PlanLimitReached, ex.Code);
        Assert.False(showcase.GetItem(b.Id)!.IsVisible);

        await service.SetItemVisibilityAsync(user.Id, a.Id, false);
        var dto = await service.SetItemVisibilityAsync(user.Id, b.Id, true);

        Assert.True(dto.Items.Single(i => i.ProductId == b.Id).IsVisible);
        Assert.Equal(1, dto.Used);
    }

    [Fact]
    public async Task SetVisible_QuoteProduct_DoesNotCheckLimit()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(limit: 1);
        var a = ctx.AddProduct(user.Id, "A");
        var quote = ctx.AddProduct(user.Id, "B");
        var showcase = ctx.AddShowcase(user.Id, "minha-loja", a, quote);
        showcase.GetItem(quote.Id)!.SetVisible(false);
        quote.SetProduced(false);

        var dto = await ctx.Service().SetItemVisibilityAsync(user.Id, quote.Id, true);

        Assert.True(dto.Items.Single(i => i.ProductId == quote.Id).IsVisible);
    }

    [Fact]
    public async Task SetVisible_MissingItem_NotFound()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        ctx.AddShowcase(user.Id, "minha-loja");

        await Assert.ThrowsAsync<NotFoundException>(() => ctx.Service().SetItemVisibilityAsync(user.Id, Guid.NewGuid(), true));
    }

    [Fact]
    public async Task Downgrade_MarksExcessAsOutsidePlan_WithoutDeleting()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(limit: 3);
        var a = ctx.AddProduct(user.Id, "A");
        var b = ctx.AddProduct(user.Id, "B");
        var c = ctx.AddProduct(user.Id, "C");
        var hidden = ctx.AddProduct(user.Id, "D");
        var showcase = ctx.AddShowcase(user.Id, "minha-loja", a, b, c, hidden);
        showcase.GetItem(hidden.Id)!.SetVisible(false);

        ctx.ChangePlan(user, limit: 1);
        var dto = Assert.IsType<ShowcaseDto>(await ctx.Service().GetAsync(user.Id));

        Assert.Equal(4, dto.Items.Count);
        Assert.Equal(3, dto.Used);
        Assert.Equal(1, dto.Limit);
        Assert.False(dto.Items.Single(i => i.ProductId == a.Id).OutsidePlan);
        Assert.True(dto.Items.Single(i => i.ProductId == b.Id).OutsidePlan);
        Assert.True(dto.Items.Single(i => i.ProductId == c.Id).OutsidePlan);
        Assert.False(dto.Items.Single(i => i.ProductId == hidden.Id).OutsidePlan);
    }

    // ---------- Ordem / remocao ----------

    [Fact]
    public async Task Reorder_RequiresExactlyCurrentItems()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        var a = ctx.AddProduct(user.Id, "A");
        var b = ctx.AddProduct(user.Id, "B");
        ctx.AddShowcase(user.Id, "minha-loja", a, b);
        var service = ctx.Service();

        await Assert.ThrowsAsync<DomainException>(() => service.ReorderItemsAsync(user.Id, new[] { a.Id }));
        await Assert.ThrowsAsync<DomainException>(() => service.ReorderItemsAsync(user.Id, new[] { a.Id, a.Id }));
        await Assert.ThrowsAsync<DomainException>(() => service.ReorderItemsAsync(user.Id, new[] { a.Id, Guid.NewGuid() }));

        var dto = await service.ReorderItemsAsync(user.Id, new[] { b.Id, a.Id });

        Assert.Equal(new[] { b.Id, a.Id }, dto.Items.Select(i => i.ProductId));
        Assert.Equal(new[] { 0, 1 }, dto.Items.Select(i => i.SortOrder));
    }

    [Fact]
    public async Task Remove_MissingItem_NotFound_ExistingRemoved()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        var a = ctx.AddProduct(user.Id);
        var showcase = ctx.AddShowcase(user.Id, "minha-loja", a);

        await Assert.ThrowsAsync<NotFoundException>(() => ctx.Service().RemoveItemAsync(user.Id, Guid.NewGuid()));
        await ctx.Service().RemoveItemAsync(user.Id, a.Id);

        Assert.Empty(showcase.Items);
    }

    // ---------- Produtos disponiveis / foto do dono ----------

    [Fact]
    public async Task AvailableProducts_OnlyOwnProducedOutsideShowcase_IgnoresHistoryWindow()
    {
        var ctx = new ShowcaseTestContext();
        var plan = ctx.Plan.AddPlan(
            Feature(PlanEntitlementKey.ShowcaseEnabled, true),
            Limit(PlanEntitlementKey.ProductHistoryMonths, 0));
        var user = ctx.Plan.AddUser(plan);
        var inShowcase = ctx.AddProduct(user.Id, "Em vitrine");
        ctx.AddShowcase(user.Id, "minha-loja", inShowcase);
        var old = ctx.AddProduct(user.Id, "Antigo").WithCreatedAt(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var recent = ctx.AddProduct(user.Id, "Bule", salePrice: 30m, quantity: 3);
        ctx.AddProduct(user.Id, "Orcamento", produced: false);
        ctx.AddProduct(ctx.AddUser().Id, "Alheio");

        var result = await ctx.Service().GetAvailableProductsAsync(user.Id, null);

        Assert.Equal(new[] { old.Id, recent.Id }, result.Select(p => p.Id));
        Assert.Equal(10m, result.Single(p => p.Id == recent.Id).UnitSalePrice);
    }

    [Fact]
    public async Task AvailableProducts_SearchFiltersAndLimitsLength()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        var vaso = ctx.AddProduct(user.Id, "Vaso grande");
        ctx.AddProduct(user.Id, "Chaveiro");

        var result = await ctx.Service().GetAvailableProductsAsync(user.Id, "  vaso ");

        Assert.Equal(vaso.Id, Assert.Single(result).Id);
        await Assert.ThrowsAsync<DomainException>(() => ctx.Service().GetAvailableProductsAsync(user.Id, new string('a', 101)));
    }

    [Fact]
    public async Task OwnerPhoto_OnlyOwnProduct_AndValidImage()
    {
        var ctx = new ShowcaseTestContext();
        var userA = ctx.AddUser();
        var userB = ctx.AddUser();
        var own = ctx.AddProduct(userA.Id, photo: TestImages.Png);
        var foreign = ctx.AddProduct(userB.Id, photo: TestImages.Png);
        var svg = ctx.AddProduct(userA.Id, photo: TestImages.Svg);

        var image = await ctx.Service().GetProductPhotoAsync(userA.Id, own.Id);

        Assert.Equal("image/png", image!.ContentType);
        Assert.Null(await ctx.Service().GetProductPhotoAsync(userA.Id, foreign.Id));
        Assert.Null(await ctx.Service().GetProductPhotoAsync(userA.Id, svg.Id));
    }

    // ---------- Admin ----------

    [Fact]
    public async Task Admin_ListsBySlugWithOwner_AndBlockMakesPublicUnavailable()
    {
        var ctx = new ShowcaseTestContext();
        var userA = ctx.AddUser();
        var userB = ctx.AddUser();
        ctx.AddShowcase(userA.Id, "zeta-loja", ctx.AddProduct(userA.Id), ctx.AddProduct(userA.Id, "X", produced: false));
        var showcaseB = ctx.AddShowcase(userB.Id, "alfa-loja");

        var list = await ctx.Service().GetAllForAdminAsync();

        Assert.Equal(new[] { "alfa-loja", "zeta-loja" }, list.Select(s => s.Slug));
        Assert.Equal(2, list[1].ItemCount);
        Assert.Equal(userA.Email, list[1].OwnerEmail);
        Assert.Equal(userA.Name, list[1].OwnerName);

        var blocked = await ctx.Service().SetBlockedAsync(showcaseB.Id, true);

        Assert.True(blocked.IsBlocked);
        Assert.IsType<PublicShowcaseUnavailableDto>(await ctx.Public().GetAsync("alfa-loja"));
        await Assert.ThrowsAsync<NotFoundException>(() => ctx.Service().SetBlockedAsync(Guid.NewGuid(), true));
    }

    [Fact]
    public async Task Owner_SeesBlockedFlag_ButCannotUnblock()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        var showcase = ctx.AddShowcase(user.Id, "minha-loja");
        showcase.SetBlocked(true);

        var dto = await ctx.Service().SetPublishedAsync(user.Id, true);

        Assert.True(dto.IsBlocked);
        Assert.IsType<PublicShowcaseUnavailableDto>(await ctx.Public().GetAsync("minha-loja"));
    }
}
