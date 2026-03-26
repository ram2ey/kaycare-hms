using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.HasKey(p => p.PrescriptionId);
        builder.Property(p => p.PrescriptionId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(p => p.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Active");
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.PrescriptionDate).HasColumnType("date");

        builder.Property(p => p.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(p => new { p.TenantId, p.PatientId });
        builder.HasIndex(p => new { p.TenantId, p.ConsultationId });
        builder.HasIndex(p => new { p.TenantId, p.Status });

        builder.HasOne(p => p.Patient)
            .WithMany()
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.PrescribedBy)
            .WithMany()
            .HasForeignKey(p => p.PrescribedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.DispensedBy)
            .WithMany()
            .HasForeignKey(p => p.DispensedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
