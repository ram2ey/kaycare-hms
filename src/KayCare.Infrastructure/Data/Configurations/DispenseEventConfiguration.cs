using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class DispenseEventConfiguration : IEntityTypeConfiguration<DispenseEvent>
{
    public void Configure(EntityTypeBuilder<DispenseEvent> builder)
    {
        builder.HasKey(e => e.DispenseEventId);
        builder.Property(e => e.DispenseEventId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasIndex(e => new { e.TenantId, e.PrescriptionId });

        builder.HasOne(e => e.Prescription)
            .WithMany(p => p.DispenseEvents)
            .HasForeignKey(e => e.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.DispensedBy)
            .WithMany()
            .HasForeignKey(e => e.DispensedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
