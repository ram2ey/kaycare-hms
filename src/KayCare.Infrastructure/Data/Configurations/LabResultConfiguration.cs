using KayCare.Core.Constants;
using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class LabResultConfiguration : IEntityTypeConfiguration<LabResult>
{
    public void Configure(EntityTypeBuilder<LabResult> builder)
    {
        builder.HasKey(r => r.LabResultId);
        builder.Property(r => r.LabResultId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(r => r.AccessionNumber).HasMaxLength(100).IsRequired();
        builder.Property(r => r.OrderCode).HasMaxLength(50);
        builder.Property(r => r.OrderName).HasMaxLength(200);
        builder.Property(r => r.Status)
               .HasMaxLength(20).IsRequired()
               .HasDefaultValue(LabResultStatus.Received);
        builder.Property(r => r.RawHl7).HasColumnType("nvarchar(max)");

        builder.Property(r => r.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(r => r.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // Accession number must be unique per tenant
        builder.HasIndex(r => new { r.TenantId, r.AccessionNumber }).IsUnique();
        builder.HasIndex(r => new { r.TenantId, r.PatientId });

        builder.HasOne(r => r.Patient)
               .WithMany()
               .HasForeignKey(r => r.PatientId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.OrderingDoctor)
               .WithMany()
               .HasForeignKey(r => r.OrderingDoctorUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Observations)
               .WithOne(o => o.LabResult)
               .HasForeignKey(o => o.LabResultId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
