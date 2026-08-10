using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintFlow.Domain.Entities;

namespace PrintFlow.Infrastructure.Persistence.Configurations;

public class FilamentTypeConfiguration : IEntityTypeConfiguration<FilamentType>
{
    public void Configure(EntityTypeBuilder<FilamentType> builder)
    {
        builder.ToTable("filaments_type");

        builder.HasKey(ft => ft.Id);

        builder.Property(ft => ft.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(ft => ft.IsActive)
            .IsRequired();

        builder.Property(ft => ft.CreatedAt)
            .IsRequired();
    }
}
