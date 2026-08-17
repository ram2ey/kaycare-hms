using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> builder)
    {
        builder.HasKey(r => r.ReferralId);
        builder.Property(r => r.ReferralId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.ToTable(t => t.HasCheckConstraint("CK_Referrals_Status",
            "\"Status\" IN ('Draft','Sent','Accepted','Completed','Declined','Cancelled')"));

        builder.Property(r => r.ReferralNumber).HasMaxLength(20).IsRequired();
        builder.Property(r => r.ReferralType).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Urgency).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Status).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(r => r.ClinicalNotes).HasMaxLength(4000);
        builder.Property(r => r.ExternalFacility).HasMaxLength(200);
        builder.Property(r => r.ReferredToDepartment).HasMaxLength(100);
        builder.Property(r => r.ResponseNotes).HasMaxLength(2000);

        builder.Property(r => r.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(r => r.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(r => new { r.TenantId, r.ReferralNumber }).IsUnique();
        builder.HasIndex(r => new { r.TenantId, r.PatientId, r.Status });
        builder.HasIndex(r => new { r.TenantId, r.ReferredToDoctorUserId, r.Status });
        builder.HasIndex(r => new { r.TenantId, r.ReferringDoctorUserId });

        builder.HasOne(r => r.Patient)
            .WithMany().HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Consultation)
            .WithMany().HasForeignKey(r => r.ConsultationId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(r => r.ReferringDoctor)
            .WithMany().HasForeignKey(r => r.ReferringDoctorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.ReferredToDoctor)
            .WithMany().HasForeignKey(r => r.ReferredToDoctorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
