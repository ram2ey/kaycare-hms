using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class RevokedTokenConfiguration : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> builder)
    {
        builder.HasKey(r => r.RevokedTokenId);
        builder.Property(r => r.RevokedTokenId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.HasIndex(r => r.Jti).IsUnique();
        builder.HasIndex(r => r.ExpiresAt);
    }
}
