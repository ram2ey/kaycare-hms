using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class BedConfiguration : IEntityTypeConfiguration<Bed>
{
    public void Configure(EntityTypeBuilder<Bed> builder)
    {
        builder.HasKey(b => b.BedId);
        builder.Property(b => b.BedId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.ToTable(t => t.HasCheckConstraint("CK_Beds_Status", "\"Status\" IN ('Available','Occupied','Maintenance')"));

        builder.Property(b => b.BedNumber).HasMaxLength(20).IsRequired();
        builder.Property(b => b.Status).HasMaxLength(20).IsRequired();
        builder.Property(b => b.Notes).HasMaxLength(500);

        builder.Property(b => b.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(b => b.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(b => new { b.TenantId, b.WardId, b.BedNumber }).IsUnique();
    }
}
