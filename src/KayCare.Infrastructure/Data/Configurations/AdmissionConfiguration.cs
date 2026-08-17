using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class AdmissionConfiguration : IEntityTypeConfiguration<Admission>
{
    public void Configure(EntityTypeBuilder<Admission> builder)
    {
        builder.HasKey(a => a.AdmissionId);
        builder.Property(a => a.AdmissionId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.ToTable(t => t.HasCheckConstraint("CK_Admissions_Status", "\"Status\" IN ('Active','Discharged')"));

        builder.Property(a => a.AdmissionNumber).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Status).HasMaxLength(20).IsRequired();
        builder.Property(a => a.AdmissionReason).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.DiagnosisOnAdmission).HasMaxLength(1000);
        builder.Property(a => a.DischargeNotes).HasMaxLength(2000);
        builder.Property(a => a.DischargeType).HasMaxLength(20);
        builder.Property(a => a.FinalDiagnosis).HasMaxLength(1000);
        builder.Property(a => a.TreatmentSummary).HasMaxLength(4000);
        builder.Property(a => a.ProceduresPerformed).HasMaxLength(2000);
        builder.Property(a => a.DischargeMedications).HasMaxLength(2000);
        builder.Property(a => a.DischargeCondition).HasMaxLength(50);
        builder.Property(a => a.FollowUpInstructions).HasMaxLength(2000);
        builder.Property(a => a.AttendingPhysicianNotes).HasMaxLength(2000);

        builder.Property(a => a.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(a => a.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(a => new { a.TenantId, a.AdmissionNumber }).IsUnique();
        builder.HasIndex(a => new { a.TenantId, a.PatientId, a.Status });
        builder.HasIndex(a => new { a.TenantId, a.BedId, a.Status });

        builder.HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Bed).WithMany().HasForeignKey(a => a.BedId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Ward).WithMany().HasForeignKey(a => a.WardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.AdmittingDoctor).WithMany().HasForeignKey(a => a.AdmittingDoctorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.CreatedBy).WithMany().HasForeignKey(a => a.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Transfers).WithOne(t => t.Admission).HasForeignKey(t => t.AdmissionId).OnDelete(DeleteBehavior.Cascade);
    }
}
