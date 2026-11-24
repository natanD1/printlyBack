using Printly.Domain.Entities;
using Printly.Domain.Exceptions;

namespace Printly.Domain.Tests;

public class ShowcaseSlugTests
{
    [Theory]
    [InlineData("Ateliê 3D", "atelie-3d")]
    [InlineData("Ação & Coração Ltda.", "acao-coracao-ltda")]
    [InlineData("  --Já É!!  ", "ja-e")]
    [InlineData("Loja   do   Zé", "loja-do-ze")]
    [InlineData("ÇÃÕ123", "cao123")]
    [InlineData("", "loja")]
    [InlineData(null, "loja")]
    [InlineData("!!!", "loja")]
    [InlineData("Zé", "loja")]
    [InlineData("日本語", "loja")]
    public void Generate_NormalizesName(string? name, string expected) =>
        Assert.Equal(expected, ShowcaseSlug.Generate(name));

    [Fact]
    public void Generate_CutsAt40_WithoutTrailingHyphen()
    {
        var slug = ShowcaseSlug.Generate(new string('a', 39) + " bcd");

        Assert.Equal(new string('a', 39), slug);
        Assert.True(ShowcaseSlug.IsValid(slug));
        Assert.True(ShowcaseSlug.Generate(new string('x', 100)).Length == 40);
    }

    [Fact]
    public void Generate_ReservedName_IsReturnedForCallerToSuffix()
    {
        Assert.Equal("admin", ShowcaseSlug.Generate("Admin"));
        Assert.True(ShowcaseSlug.IsReserved("admin"));
        Assert.Equal("loja-2", ShowcaseSlug.WithSuffix("loja", 2));
        Assert.Equal("loja", ShowcaseSlug.WithSuffix("loja", 1));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("minha-loja")]
    [InlineData("loja-2")]
    [InlineData("123")]
    public void IsValid_Accepts(string slug) => Assert.True(ShowcaseSlug.IsValid(slug));

    [Theory]
    [InlineData("../etc")]
    [InlineData("..")]
    [InlineData("%2e%2e")]
    [InlineData("minha loja")]
    [InlineData("Minha-Loja")]
    [InlineData("lojã")]
    [InlineData("lojá")]
    [InlineData("ab")]
    [InlineData("-loja")]
    [InlineData("loja-")]
    [InlineData("lo--ja")]
    [InlineData("minha-loja\n")]
    [InlineData("minha_loja")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_Rejects(string? slug) => Assert.False(ShowcaseSlug.IsValid(slug));

    [Fact]
    public void IsValid_RejectsOver60()
    {
        Assert.True(ShowcaseSlug.IsValid(new string('a', 60)));
        Assert.False(ShowcaseSlug.IsValid(new string('a', 61)));
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("api")]
    [InlineData("auth")]
    [InlineData("loja")]
    [InlineData("www")]
    [InlineData("printly")]
    [InlineData("suporte")]
    [InlineData("ajuda")]
    public void Showcase_RejectsReservedSlug(string slug)
    {
        var ex = Assert.Throws<DomainException>(() => new Showcase(Guid.NewGuid(), slug));

        Assert.Equal(ShowcaseSlug.ReservedMessage, ex.Message);
    }

    [Fact]
    public void Showcase_NewIsPublishedAndNotBlocked()
    {
        var showcase = new Showcase(Guid.NewGuid(), "minha-loja");

        Assert.True(showcase.IsPublished);
        Assert.False(showcase.IsBlocked);
        Assert.Empty(showcase.Items);
    }
}
