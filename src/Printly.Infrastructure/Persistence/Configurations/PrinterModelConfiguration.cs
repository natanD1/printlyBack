using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Printly.Domain.Entities;

namespace Printly.Infrastructure.Persistence.Configurations;

public class PrinterModelConfiguration : IEntityTypeConfiguration<PrinterModel>
{
    public void Configure(EntityTypeBuilder<PrinterModel> builder)
    {
        builder.ToTable("printer_models");

        builder.HasKey(pm => pm.Id);

        builder.Property(pm => pm.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(pm => pm.BrandId)
            .IsRequired();

        builder.Property(pm => pm.Image)
            .HasColumnType("longtext");

        builder.Property(pm => pm.IsActive)
            .IsRequired();

        builder.Property(pm => pm.CreatedAt)
            .IsRequired();

        builder.HasOne<PrinterBrand>()
            .WithMany()
            .HasForeignKey(pm => pm.BrandId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
