using Microsoft.EntityFrameworkCore;
using Printly.Domain.Entities;
using Printly.Infrastructure.Persistence;

namespace Printly.Application.Tests;

/// <summary>
/// Trava a configuracao EF de <see cref="Plan.IsPublic"/> sem banco: com HasDefaultValue(true), o sentinel tambem
/// precisa ser true; senao o EF 8 omite o false no INSERT e o banco grava true (plano criado oculto sai publico).
/// </summary>
public class PlanPersistenceModelTests
{
    [Fact]
    public void IsPublic_HasDefaultTrue_AndSentinelTrue()
    {
        var options = new DbContextOptionsBuilder<PrintlyDbContext>()
            .UseMySql("Server=localhost;Database=model_only", new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        using var context = new PrintlyDbContext(options);
        var property = context.Model.FindEntityType(typeof(Plan))!.FindProperty(nameof(Plan.IsPublic))!;

        Assert.Equal(true, property.GetDefaultValue());
        Assert.Equal(true, property.Sentinel);
        Assert.False(property.IsNullable);
    }
}
