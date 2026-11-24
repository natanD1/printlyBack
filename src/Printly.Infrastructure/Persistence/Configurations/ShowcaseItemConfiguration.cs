using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Printly.Domain.Entities;

namespace Printly.Infrastructure.Persistence.Configurations;

public class ShowcaseItemConfiguration : IEntityTypeConfiguration<ShowcaseItem>
{
    public void Configure(EntityTypeBuilder<ShowcaseItem> builder)
    {
        builder.ToTable("showcase_items");

        builder.HasKey(i => i.Id);

        // Id gerado no dominio: item novo achado pela navegacao da vitrine entra como Added (e nao Modified).
        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.SortOrder)
            .IsRequired();

        builder.Property(i => i.IsVisible)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.HasIndex(i => new { i.ShowcaseId, i.ProductId })
            .IsUnique();

        // Apagar o produto tira o item da vitrine.
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
