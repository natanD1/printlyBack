using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Printly.Domain.Entities;

namespace Printly.Infrastructure.Persistence.Configurations;

public class FilamentConfiguration : IEntityTypeConfiguration<Filament>
{
    public void Configure(EntityTypeBuilder<Filament> builder)
    {
        builder.ToTable("filaments");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(f => f.BrandId)
            .IsRequired();

        builder.Property(f => f.TypeId)
            .IsRequired();

        builder.Property(f => f.ColorHex)
            .HasMaxLength(7)
            .IsRequired();

        builder.Property(f => f.Weight)
            .HasColumnType("decimal(10,3)")
            .IsRequired();

        builder.Property(f => f.FilamentPrice)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(f => f.UserId)
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<FilamentBrand>()
            .WithMany()
            .HasForeignKey(f => f.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FilamentType>()
            .WithMany()
            .HasForeignKey(f => f.TypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
