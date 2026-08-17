using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class MedicationAdministrationConfiguration : IEntityTypeConfiguration<MedicationAdministration>
{
    public void Configure(EntityTypeBuilder<MedicationAdministration> b)
    {
        b.HasKey(m => m.MedicationAdministrationId);
        b.Property(m => m.MedicationAdministrationId).HasDefaultValueSql("NEWSEQUENTIALID()");

        b.ToTable(t => t.HasCheckConstraint("CK_MedicationAdministrations_Status",
            "\"Status\" IN ('Given','Held','Refused','NotAvailable')"));

        b.Property(m => m.Status).HasMaxLength(20).IsRequired();
        b.Property(m => m.DoseGiven).HasMaxLength(100);
        b.Property(m => m.Route).HasMaxLength(50);
        b.Property(m => m.Notes).HasMaxLength(500);

        b.HasIndex(m => new { m.TenantId, m.PatientId, m.AdministeredAt });
        b.HasIndex(m => new { m.TenantId, m.AdmissionId });
        b.HasIndex(m => new { m.TenantId, m.PrescriptionItemId });

        b.HasOne(m => m.Prescription).WithMany().HasForeignKey(m => m.PrescriptionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(m => m.PrescriptionItem).WithMany().HasForeignKey(m => m.PrescriptionItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(m => m.Patient).WithMany().HasForeignKey(m => m.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(m => m.AdministeredBy).WithMany().HasForeignKey(m => m.AdministeredByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(m => m.Admission).WithMany().HasForeignKey(m => m.AdmissionId).OnDelete(DeleteBehavior.Restrict);
    }
}
