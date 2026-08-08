using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintFlow.Domain.Entities;

namespace PrintFlow.Infrastructure.Persistence.Configurations;

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
