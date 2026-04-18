using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class VitalSignsConfiguration : IEntityTypeConfiguration<VitalSigns>
{
    public void Configure(EntityTypeBuilder<VitalSigns> b)
    {
        b.HasKey(v => v.VitalSignsId);
        b.Property(v => v.VitalSignsId).HasDefaultValueSql("NEWSEQUENTIALID()");

        b.Property(v => v.Temperature).HasColumnType("decimal(5,2)");
        b.Property(v => v.Weight).HasColumnType("decimal(6,2)");
        b.Property(v => v.Height).HasColumnType("decimal(5,2)");
        b.Property(v => v.Notes).HasMaxLength(500);

        b.HasIndex(v => new { v.TenantId, v.PatientId, v.RecordedAt });
        b.HasIndex(v => new { v.TenantId, v.AdmissionId });

        b.HasOne(v => v.Patient).WithMany().HasForeignKey(v => v.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(v => v.RecordedBy).WithMany().HasForeignKey(v => v.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(v => v.Admission).WithMany().HasForeignKey(v => v.AdmissionId).OnDelete(DeleteBehavior.Restrict);
    }
}
