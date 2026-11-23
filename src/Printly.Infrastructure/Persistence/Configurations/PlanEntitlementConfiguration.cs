using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Printly.Domain.Entities;

namespace Printly.Infrastructure.Persistence.Configurations;

public class PlanEntitlementConfiguration : IEntityTypeConfiguration<PlanEntitlement>
{
    public void Configure(EntityTypeBuilder<PlanEntitlement> builder)
    {
        builder.ToTable("plan_entitlements");

        builder.HasKey(e => new { e.PlanId, e.Key });

        // Gravado como texto: renomear/reordenar o enum nao corrompe dados (nunca renomear um valor existente).
        builder.Property(e => e.Key)
            .HasConversion<string>()
            .HasMaxLength(60)
            .IsRequired();
    }
}
