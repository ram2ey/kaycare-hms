using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class InpatientChargeConfiguration : IEntityTypeConfiguration<InpatientCharge>
{
    public void Configure(EntityTypeBuilder<InpatientCharge> builder)
    {
        builder.HasKey(c => c.InpatientChargeId);
        builder.Property(c => c.InpatientChargeId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.ToTable(t => t
            .HasCheckConstraint("CK_InpatientCharges_Quantity_Positive", "\"Quantity\" > 0")
            .HasCheckConstraint("CK_InpatientCharges_UnitPrice_NonNegative", "\"UnitPrice\" >= 0"));

        builder.Property(c => c.Category).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500).IsRequired();
        builder.Property(c => c.UnitPrice).HasColumnType("decimal(12,2)");
        builder.Property(c => c.TotalPrice).HasColumnType("decimal(12,2)");
        builder.Property(c => c.Notes).HasMaxLength(500);

        builder.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(c => new { c.TenantId, c.AdmissionId, c.ChargeDate });

        // Billing history must never silently disappear if the admission is deleted — matches
        // the Restrict behavior already used by every other Admission child record
        // (VitalSigns, NursingNote, MedicationAdministration).
        builder.HasOne(c => c.Admission)
            .WithMany()
            .HasForeignKey(c => c.AdmissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CreatedBy)
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
