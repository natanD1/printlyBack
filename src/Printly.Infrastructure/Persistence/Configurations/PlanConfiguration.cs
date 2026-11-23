using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Printly.Domain.Entities;

namespace Printly.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(Plan.NameMaxLength)
            .IsRequired();

        // Collation padrao do MySQL nao diferencia maiusculas: o indice reforca a checagem do PlanService.
        builder.HasIndex(p => p.Name)
            .IsUnique();

        builder.Property(p => p.Description)
            .HasMaxLength(Plan.DescriptionMaxLength);

        builder.Property(p => p.MonthlyPrice)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        // Default no banco: planos ja existentes viram publicos na migration. Sem o sentinel, o EF trataria
        // false (default do bool) como "nao informado" e o banco gravaria true num plano criado oculto.
        builder.Property(p => p.IsPublic)
            .IsRequired()
            .HasDefaultValue(true)
            .HasSentinel(true);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasMany(p => p.Entitlements)
            .WithOne()
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Entitlements)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
