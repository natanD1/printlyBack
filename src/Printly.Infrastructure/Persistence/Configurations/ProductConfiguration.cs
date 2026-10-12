using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Printly.Domain.Entities;

namespace Printly.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.ProductPhoto)
            .HasColumnType("longtext");

        builder.Property(p => p.TotalHours)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(p => p.TotalFilament)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(p => p.CostPrice)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(p => p.SalePrice)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Filaments)
            .WithOne()
            .HasForeignKey(pf => pf.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Filaments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
