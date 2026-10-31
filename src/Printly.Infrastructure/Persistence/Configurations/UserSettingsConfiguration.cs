using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Printly.Domain.Entities;

namespace Printly.Infrastructure.Persistence.Configurations;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("user_settings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.KwhPrice)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(s => s.AveragePowerWatts)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(s => s.DefaultProfitMarginPercentage)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(s => s.PrinterValue)
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(s => s.PrinterLifespanHours)
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(2000m)
            .IsRequired();

        builder.Property(s => s.MaintenancePercentage)
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(10m)
            .IsRequired();

        builder.Property(s => s.FailureRatePercentage)
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(5m)
            .IsRequired();

        builder.Property(s => s.DepreciationEnabled)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(s => s.MaintenanceEnabled)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(s => s.FailureEnabled)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.HasIndex(s => s.UserId)
            .IsUnique();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
