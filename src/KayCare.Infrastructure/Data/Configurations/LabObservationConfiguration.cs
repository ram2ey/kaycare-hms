using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class LabObservationConfiguration : IEntityTypeConfiguration<LabObservation>
{
    public void Configure(EntityTypeBuilder<LabObservation> builder)
    {
        builder.HasKey(o => o.LabObservationId);
        builder.Property(o => o.LabObservationId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(o => o.TestCode).HasMaxLength(50).IsRequired();
        builder.Property(o => o.TestName).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Value).HasMaxLength(500);
        builder.Property(o => o.Units).HasMaxLength(50);
        builder.Property(o => o.ReferenceRange).HasMaxLength(100);
        builder.Property(o => o.AbnormalFlag).HasMaxLength(5);

        builder.HasIndex(o => new { o.TenantId, o.LabResultId });
    }
}
