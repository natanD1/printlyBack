using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Printly.Domain.Entities;

namespace Printly.Infrastructure.Persistence.Configurations;

public class ShowcaseConfiguration : IEntityTypeConfiguration<Showcase>
{
    public void Configure(EntityTypeBuilder<Showcase> builder)
    {
        builder.ToTable("showcases");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        // Uma vitrine por usuario: o indice segura a corrida de dois POST /showcase.
        builder.HasIndex(s => s.UserId)
            .IsUnique();

        builder.Property(s => s.Slug)
            .HasColumnType($"varchar({ShowcaseSlug.MaxLength})")
            .HasMaxLength(ShowcaseSlug.MaxLength)
            .IsRequired();

        // Unicidade de verdade fica no banco: a checagem do servico nao cobre corrida (1062 -> "ja esta em uso").
        builder.HasIndex(s => s.Slug)
            .IsUnique();

        builder.Property(s => s.IsPublished)
            .IsRequired();

        builder.Property(s => s.IsBlocked)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.ShowcaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
