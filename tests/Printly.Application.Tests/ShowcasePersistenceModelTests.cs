using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Printly.Domain.Entities;
using Printly.Infrastructure.Persistence;
using Printly.Infrastructure.Persistence.Repositories;

namespace Printly.Application.Tests;

/// <summary>
/// Configuracao EF da vitrine e traducao das consultas pro MySQL, sem banco (ToQueryString nao abre conexao).
/// </summary>
public class ShowcasePersistenceModelTests
{
    private static PrintlyDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<PrintlyDbContext>()
            .UseMySql("Server=localhost;Database=model_only", new MySqlServerVersion(new Version(8, 0, 36)))
            .Options);

    [Fact]
    public void Showcase_UniqueUserAndSlug_Varchar60_CascadeFromUser()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Showcase))!;

        Assert.Equal("showcases", entity.GetTableName());
        Assert.Equal("varchar(60)", entity.FindProperty(nameof(Showcase.Slug))!.GetColumnType());
        Assert.True(entity.GetIndexes().Single(i => i.Properties.Single().Name == nameof(Showcase.Slug)).IsUnique);
        Assert.True(entity.GetIndexes().Single(i => i.Properties.Single().Name == nameof(Showcase.UserId)).IsUnique);
        Assert.Equal(DeleteBehavior.Cascade, entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(User)).DeleteBehavior);
    }

    [Fact]
    public void ShowcaseItem_UniquePair_CascadeFromShowcaseAndProduct_ClientId()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ShowcaseItem))!;

        Assert.Equal("showcase_items", entity.GetTableName());
        Assert.True(entity.GetIndexes().Single(i => i.Properties.Count == 2).IsUnique);
        Assert.All(entity.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Cascade, fk.DeleteBehavior));
        Assert.Contains(entity.GetForeignKeys(), fk => fk.PrincipalEntityType.ClrType == typeof(Product));
        Assert.Equal(ValueGenerated.Never, entity.FindProperty(nameof(ShowcaseItem.Id))!.ValueGenerated);
    }

    [Fact]
    public void Product_UnitSalePrice_IsNotMapped()
    {
        using var context = CreateContext();

        Assert.Null(context.Model.FindEntityType(typeof(Product))!.FindProperty(nameof(Product.UnitSalePrice)));
    }

    [Fact]
    public void ItemsAddedAndRemovedThroughShowcase_TrackedAsAddedAndDeleted()
    {
        using var context = CreateContext();
        var showcase = new Showcase(Guid.NewGuid(), "minha-loja");
        showcase.AddItems(new[] { Guid.NewGuid() });
        context.Attach(showcase);
        var existing = showcase.Items.Single();

        var added = showcase.AddItems(new[] { Guid.NewGuid() }).Single();
        showcase.RemoveItem(existing.ProductId);
        context.ChangeTracker.DetectChanges();

        Assert.Equal(EntityState.Added, context.Entry(added).State);
        Assert.Equal(EntityState.Deleted, context.Entry(existing).State);
    }

    [Fact]
    public void Queries_TranslateToSql_WithParameterizedSearch()
    {
        using var context = CreateContext();
        var repository = new ShowcaseRepository(context);
        const string term = "x%' OR 1=1 -- ";

        var summaries = repository.QueryProductSummaries(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() }).ToQueryString();
        var count = repository.QueryVisibleProducedItems(Guid.NewGuid()).ToQueryString();

        // ToQueryString() do Pomelo mostra parametros string inline (so na exibicao). O comando real
        // (CreateDbCommand, sem abrir conexao) e o que vai pro MySQL: o padrao do LIKE e parametro.
        using var command = repository.QueryAvailableProducts(Guid.NewGuid(), term).CreateDbCommand();
        var sql = command.CommandText;
        var parameters = command.Parameters.Cast<DbParameter>().ToList();

        Assert.Matches(@"`ProductName` LIKE @__pattern_\d+ ESCAPE", sql);
        Assert.DoesNotContain("OR 1=1", sql);
        Assert.DoesNotContain("x%", sql);
        var pattern = Assert.Single(parameters, p => p.ParameterName.StartsWith("@__pattern_", StringComparison.Ordinal));
        Assert.Equal(@"%x\%' OR 1=1 -- %", pattern.Value);
        Assert.DoesNotContain("ProductPhoto` AS", sql);
        Assert.Contains("ProductName", sql);
        Assert.Contains("ProductPhoto", summaries);
        Assert.DoesNotContain("ProductPhoto` AS", summaries);
        Assert.Contains("IsProduced", count);
    }

    [Fact]
    public void AvailableProducts_WithoutSearch_HasNoLike()
    {
        using var context = CreateContext();
        using var command = new ShowcaseRepository(context).QueryAvailableProducts(Guid.NewGuid(), null).CreateDbCommand();

        Assert.DoesNotContain("LIKE", command.CommandText);
    }
}
