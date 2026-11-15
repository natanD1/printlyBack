using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Printly.Domain.Entities;

namespace Printly.Infrastructure.Persistence.Configurations;

public class PrinterBrandConfiguration : IEntityTypeConfiguration<PrinterBrand>
{
    public void Configure(EntityTypeBuilder<PrinterBrand> builder)
    {
        builder.ToTable("printer_brands");

        builder.HasKey(pb => pb.Id);

        builder.Property(pb => pb.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(pb => pb.IsActive)
            .IsRequired();

        builder.Property(pb => pb.CreatedAt)
            .IsRequired();
    }
}
