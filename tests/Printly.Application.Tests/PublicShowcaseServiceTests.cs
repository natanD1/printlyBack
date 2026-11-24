using System.Text.Json;
using Printly.Application.DTOs.Showcases;
using Printly.Domain.Exceptions;

namespace Printly.Application.Tests;

/// <summary>Pagina publica: disponibilidade, allowlist do JSON, corte do plano e isolamento entre contas.</summary>
public class PublicShowcaseServiceTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static string Serialize(PublicShowcaseResponse response) => JsonSerializer.Serialize<object>(response, WebJson);

    [Fact]
    public async Task UnknownSlug_NotFound()
    {
        var ctx = new ShowcaseTestContext();

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => ctx.Public().GetAsync("nao-existe"));

        Assert.Equal("Vitrine nao encontrada.", ex.Message);
    }

    [Theory]
    [InlineData("MINHA-LOJA")]
    [InlineData("../minha-loja")]
    [InlineData("minha-loja\n")]
    [InlineData("ab")]
    public async Task InvalidSlug_NotFound_WithoutQueryingRepository(string slug)
    {
        var ctx = new ShowcaseTestContext();
        ctx.AddShowcase(ctx.AddUser().Id, "minha-loja");

        await Assert.ThrowsAsync<NotFoundException>(() => ctx.Public().GetAsync(slug));
        Assert.Null(await ctx.Public().GetLogoAsync(slug));
        Assert.Null(await ctx.Public().GetItemPhotoAsync(slug, Guid.NewGuid()));
        Assert.Equal(0, ctx.Showcases.SlugLookups);
    }

    [Theory]
    [InlineData("unpublished")]
    [InlineData("blocked")]
    [InlineData("plan-off")]
    public async Task Unavailable_SerializesExactlyAvailableFalse(string reason)
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(showcaseEnabled: reason != "plan-off", logo: true);
        var product = ctx.AddProduct(user.Id, photo: TestImages.Png);
        ctx.AddCompany(user.Id, logo: TestImages.Png);
        var showcase = ctx.AddShowcase(user.Id, "minha-loja", product);

        if (reason == "unpublished")
            showcase.SetPublished(false);
        if (reason == "blocked")
            showcase.SetBlocked(true);

        var response = await ctx.Public().GetAsync("minha-loja");

        Assert.Equal("{\"available\":false}", Serialize(response));
        Assert.Null(await ctx.Public().GetLogoAsync("minha-loja"));
        Assert.Null(await ctx.Public().GetItemPhotoAsync("minha-loja", product.Id));
    }

    [Fact]
    public async Task Available_SerializesOnlyAllowlistedFields()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(logo: true);
        ctx.AddCompany(user.Id, "Ateliê 3D", TestImages.Png, "(11) 4002-8922", "contato@atelie.com", "(11) 99999-8888", "@atelie3d", "atelie3d.com.br");
        var product = ctx.AddProduct(user.Id, "Vaso", photo: TestImages.Jpeg, salePrice: 51m, quantity: 2);
        ctx.AddShowcase(user.Id, "minha-loja", product);

        var json = Serialize(await ctx.Public().GetAsync("minha-loja"));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(
            new[] { "available", "contacts", "hasLogo", "items", "showBranding", "storeName" },
            root.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        Assert.Equal(
            new[] { "email", "instagram", "phone", "website", "whatsApp" },
            root.GetProperty("contacts").EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        var item = Assert.Single(root.GetProperty("items").EnumerateArray());
        Assert.Equal(
            new[] { "hasPhoto", "name", "productId", "unitPrice" },
            item.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));

        Assert.True(root.GetProperty("available").GetBoolean());
        Assert.Equal("Ateliê 3D", root.GetProperty("storeName").GetString());
        Assert.Equal(25.5m, item.GetProperty("unitPrice").GetDecimal());
        Assert.Equal("5511999998888", root.GetProperty("contacts").GetProperty("whatsApp").GetString());
        Assert.Equal("atelie3d", root.GetProperty("contacts").GetProperty("instagram").GetString());
        Assert.Equal("https://atelie3d.com.br/", root.GetProperty("contacts").GetProperty("website").GetString());
        Assert.DoesNotContain(user.Id.ToString(), json);
        Assert.DoesNotContain(user.Email, json);
    }

    [Fact]
    public async Task Contacts_UnsafeValuesBecomeNull()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        ctx.AddCompany(user.Id, "Loja X", phone: "ligue já", email: "nao e email", whatsApp: "123", instagram: "https://evil.com/x", website: "javascript:alert(1)");
        ctx.AddShowcase(user.Id, "loja-x");

        var dto = Assert.IsType<PublicShowcaseDto>(await ctx.Public().GetAsync("loja-x"));

        Assert.Equal(new PublicShowcaseContactsDto(null, null, null, null, null), dto.Contacts);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task HasLogo_OnlyWithPlanFeature(bool feature, bool expected)
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(logo: feature);
        ctx.AddCompany(user.Id, logo: TestImages.Png);
        ctx.AddShowcase(user.Id, "minha-loja");

        var dto = Assert.IsType<PublicShowcaseDto>(await ctx.Public().GetAsync("minha-loja"));
        var logo = await ctx.Public().GetLogoAsync("minha-loja");

        Assert.Equal(expected, dto.HasLogo);
        Assert.Equal(expected, logo is not null);
        if (expected)
            Assert.Equal("image/png", logo!.ContentType);
    }

    [Fact]
    public async Task HasLogo_FalseWithoutLogo_EvenWithFeature()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(logo: true);
        ctx.AddCompany(user.Id);
        ctx.AddShowcase(user.Id, "minha-loja");

        var dto = Assert.IsType<PublicShowcaseDto>(await ctx.Public().GetAsync("minha-loja"));

        Assert.False(dto.HasLogo);
        Assert.Null(await ctx.Public().GetLogoAsync("minha-loja"));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ShowBranding_IsNotRemoveBrandingFooter(bool removeBranding, bool expected)
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(removeBranding: removeBranding);
        ctx.AddCompany(user.Id);
        ctx.AddShowcase(user.Id, "minha-loja");

        var dto = Assert.IsType<PublicShowcaseDto>(await ctx.Public().GetAsync("minha-loja"));

        Assert.Equal(expected, dto.ShowBranding);
    }

    [Fact]
    public async Task Items_FollowPlanCut_HiddenAndQuoteExcluded()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser(limit: 2);
        ctx.AddCompany(user.Id);
        var a = ctx.AddProduct(user.Id, "A", photo: TestImages.Png);
        var hidden = ctx.AddProduct(user.Id, "Oculto", photo: TestImages.Png);
        var quote = ctx.AddProduct(user.Id, "Orcamento", photo: TestImages.Png);
        var b = ctx.AddProduct(user.Id, "B", photo: TestImages.Png);
        var c = ctx.AddProduct(user.Id, "C", photo: TestImages.Png);
        var showcase = ctx.AddShowcase(user.Id, "minha-loja", a, hidden, quote, b, c);
        showcase.GetItem(hidden.Id)!.SetVisible(false);
        quote.SetProduced(false);

        var dto = Assert.IsType<PublicShowcaseDto>(await ctx.Public().GetAsync("minha-loja"));

        Assert.Equal(new[] { a.Id, b.Id }, dto.Items.Select(i => i.ProductId));
        Assert.NotNull(await ctx.Public().GetItemPhotoAsync("minha-loja", b.Id));
        Assert.Null(await ctx.Public().GetItemPhotoAsync("minha-loja", c.Id));
        Assert.Null(await ctx.Public().GetItemPhotoAsync("minha-loja", hidden.Id));
        Assert.Null(await ctx.Public().GetItemPhotoAsync("minha-loja", quote.Id));
    }

    [Fact]
    public async Task ProductBackToQuote_DisappearsFromPublic()
    {
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        ctx.AddCompany(user.Id);
        var product = ctx.AddProduct(user.Id, photo: TestImages.Png);
        ctx.AddShowcase(user.Id, "minha-loja", product);

        product.SetProduced(false);

        var dto = Assert.IsType<PublicShowcaseDto>(await ctx.Public().GetAsync("minha-loja"));
        Assert.Empty(dto.Items);
        Assert.Null(await ctx.Public().GetItemPhotoAsync("minha-loja", product.Id));
    }

    [Fact]
    public async Task Photo_SlugOfA_NeverServesProductOfB()
    {
        var ctx = new ShowcaseTestContext();
        var userA = ctx.AddUser();
        var userB = ctx.AddUser();
        ctx.AddShowcase(userA.Id, "loja-a", ctx.AddProduct(userA.Id, photo: TestImages.Png));
        var productOfB = ctx.AddProduct(userB.Id, photo: TestImages.Png);
        ctx.AddShowcase(userB.Id, "loja-b", productOfB);

        Assert.Null(await ctx.Public().GetItemPhotoAsync("loja-a", productOfB.Id));
        Assert.NotNull(await ctx.Public().GetItemPhotoAsync("loja-b", productOfB.Id));
    }

    [Theory]
    [InlineData("svg")]
    [InlineData("garbage")]
    [InlineData("not-base64")]
    public async Task Photo_InvalidImage_Null(string kind)
    {
        var photo = kind switch
        {
            "svg" => TestImages.Svg,
            "garbage" => TestImages.Garbage,
            _ => "%%%nao-e-base64%%%"
        };
        var ctx = new ShowcaseTestContext();
        var user = ctx.AddUser();
        var product = ctx.AddProduct(user.Id, photo: photo);
        ctx.AddShowcase(user.Id, "minha-loja", product);

        Assert.Null(await ctx.Public().GetItemPhotoAsync("minha-loja", product.Id));
    }
}
