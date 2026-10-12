using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Printly.Domain.Entities;

namespace Printly.Infrastructure.Persistence.Configurations;

public class ProductFilamentConfiguration : IEntityTypeConfiguration<ProductFilament>
{
    public void Configure(EntityTypeBuilder<ProductFilament> builder)
    {
        builder.ToTable("product_filaments");

        builder.HasKey(pf => new { pf.ProductId, pf.FilamentId });

        builder.Property(pf => pf.GramsUsed)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.HasOne<Filament>()
            .WithMany()
            .HasForeignKey(pf => pf.FilamentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
