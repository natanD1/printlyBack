using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Printly.Domain.Entities;

namespace Printly.Infrastructure.Persistence.Configurations;

public class FilamentBrandConfiguration : IEntityTypeConfiguration<FilamentBrand>
{
    public void Configure(EntityTypeBuilder<FilamentBrand> builder)
    {
        builder.ToTable("filaments_brands");

        builder.HasKey(fb => fb.Id);

        builder.Property(fb => fb.NameBrand)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(fb => fb.ImageBrand)
            .HasColumnType("longtext");

        builder.Property(fb => fb.IsActive)
            .IsRequired();

        builder.Property(fb => fb.CreatedAt)
            .IsRequired();
    }
}
