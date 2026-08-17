using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class LabOrderItemConfiguration : IEntityTypeConfiguration<LabOrderItem>
{
    public void Configure(EntityTypeBuilder<LabOrderItem> builder)
    {
        builder.HasKey(i => i.LabOrderItemId);
        builder.Property(i => i.LabOrderItemId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.ToTable(t => t.HasCheckConstraint("CK_LabOrderItems_Status",
            "\"Status\" IN ('Ordered','SampleReceived','Resulted','Signed')"));

        builder.Property(i => i.TestName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Department).HasMaxLength(100).IsRequired();
        builder.Property(i => i.InstrumentType).HasMaxLength(50);
        builder.Property(i => i.AccessionNumber).HasMaxLength(50);
        builder.Property(i => i.Status).HasMaxLength(30).IsRequired();
        builder.Property(i => i.ManualResult).HasMaxLength(500);
        builder.Property(i => i.ManualResultNotes).HasMaxLength(1000);
        builder.Property(i => i.ManualResultUnit).HasMaxLength(50);
        builder.Property(i => i.ManualResultReferenceRange).HasMaxLength(100);
        builder.Property(i => i.ManualResultFlag).HasMaxLength(10);

        // Accession number unique per tenant (once assigned)
        builder.HasIndex(i => new { i.TenantId, i.AccessionNumber })
               .IsUnique()
               .HasFilter("\"AccessionNumber\" IS NOT NULL");

        // Order-lookup path — EF's automatic FK index on the bare LabOrderId column doesn't
        // include TenantId, so a tenant-scoped "items for this order" query can't use it as a
        // covering index.
        builder.HasIndex(i => new { i.TenantId, i.LabOrderId });

        builder.HasOne(i => i.LabTestCatalog)
               .WithMany()
               .HasForeignKey(i => i.LabTestCatalogId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.LabResult)
               .WithOne(r => r.LabOrderItem)
               .HasForeignKey<LabResult>(r => r.LabOrderItemId)
               .OnDelete(DeleteBehavior.SetNull);

        // CriticalCallLog documents that a critical/panic lab value was called in to a
        // clinician — a patient-safety compliance record that must never silently disappear
        // if the parent order item is deleted.
        builder.HasOne(i => i.CriticalCallLog)
               .WithOne(c => c.LabOrderItem)
               .HasForeignKey<CriticalCallLog>(c => c.LabOrderItemId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
