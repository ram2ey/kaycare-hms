using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class WardConfiguration : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> builder)
    {
        builder.HasKey(w => w.WardId);
        builder.Property(w => w.WardId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.WardType).HasMaxLength(50).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(500);
        builder.Property(w => w.DailyRate).HasColumnType("decimal(12,2)");

        builder.Property(w => w.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(w => w.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(w => new { w.TenantId, w.Name }).IsUnique();

        builder.HasMany(w => w.Beds)
               .WithOne(b => b.Ward)
               .HasForeignKey(b => b.WardId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
