using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintFlow.Domain.Entities;

namespace PrintFlow.Infrastructure.Persistence.Configurations;

public class FilamentConfiguration : IEntityTypeConfiguration<Filament>
{
    public void Configure(EntityTypeBuilder<Filament> builder)
    {
        builder.ToTable("filaments");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(f => f.Brand)
            .HasMaxLength(150)
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
    }
}
