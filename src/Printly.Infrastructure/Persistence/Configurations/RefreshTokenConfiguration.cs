using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Printly.Domain.Entities;

namespace Printly.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.TokenHash)
            .HasMaxLength(RefreshToken.TokenHashLength)
            .IsFixedLength()
            .IsRequired();

        builder.HasIndex(r => r.TokenHash)
            .IsUnique();

        builder.Property(r => r.FamilyId)
            .IsRequired();

        builder.HasIndex(r => r.FamilyId);
        builder.HasIndex(r => r.UserId);

        builder.Property(r => r.ExpiresAt)
            .IsRequired();

        builder.Property(r => r.Revoked)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
