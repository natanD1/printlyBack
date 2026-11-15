using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Printly.Domain.Entities;

namespace Printly.Infrastructure.Persistence.Configurations;

public class PrinterConfiguration : IEntityTypeConfiguration<Printer>
{
    public void Configure(EntityTypeBuilder<Printer> builder)
    {
        builder.ToTable("printers");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PrinterModelId)
            .IsRequired();

        builder.Property(p => p.Nickname)
            .HasMaxLength(100);

        builder.Property(p => p.AveragePowerWatts)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(p => p.DefaultProfitMarginPercentage)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(p => p.PrinterValue)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(p => p.PrinterLifespanHours)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(p => p.MaintenancePercentage)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(p => p.FailureRatePercentage)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(p => p.DepreciationEnabled)
            .IsRequired();

        builder.Property(p => p.MaintenanceEnabled)
            .IsRequired();

        builder.Property(p => p.FailureEnabled)
            .IsRequired();

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.HasIndex(p => p.UserId);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<PrinterModel>()
            .WithMany()
            .HasForeignKey(p => p.PrinterModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
