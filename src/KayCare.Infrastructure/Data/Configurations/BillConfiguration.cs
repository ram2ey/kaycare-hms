using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.HasKey(b => b.BillId);
        builder.Property(b => b.BillId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Bills_Amounts_NonNegative",
                "\"TotalAmount\" >= 0 AND \"DiscountAmount\" >= 0 AND \"WriteOffAmount\" >= 0 AND \"PaidAmount\" >= 0 AND \"CreditNoteTotal\" >= 0");
            t.HasCheckConstraint("CK_Bills_Status",
                "\"Status\" IN ('Draft','Issued','PartiallyPaid','Paid','Cancelled','Void','WrittenOff')");
        });

        builder.Property(b => b.BillNumber).HasMaxLength(20).IsRequired();
        builder.Property(b => b.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Draft");
        builder.Property(b => b.Notes).HasMaxLength(1000);

        builder.Property(b => b.TotalAmount).HasColumnType("decimal(12,2)");
        builder.Property(b => b.AdjustmentTotal).HasColumnType("decimal(12,2)").HasDefaultValue(0m);
        builder.Property(b => b.DiscountAmount).HasColumnType("decimal(12,2)").HasDefaultValue(0m);
        builder.Property(b => b.DiscountReason).HasMaxLength(500);
        builder.Property(b => b.WriteOffAmount).HasColumnType("decimal(12,2)").HasDefaultValue(0m);
        builder.Property(b => b.WriteOffReason).HasMaxLength(500);
        builder.Property(b => b.CreditNoteTotal).HasColumnType("decimal(12,2)").HasDefaultValue(0m);
        builder.Property(b => b.PaidAmount).HasColumnType("decimal(12,2)");
        builder.Property(b => b.BalanceDue)
            .HasColumnType("decimal(12,2)")
            .HasComputedColumnSql("[TotalAmount] + [AdjustmentTotal] - [DiscountAmount] - [WriteOffAmount] - [CreditNoteTotal] - [PaidAmount]", stored: true);

        builder.Property(b => b.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(b => b.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(b => new { b.TenantId, b.BillNumber }).IsUnique();
        builder.HasIndex(b => new { b.TenantId, b.PatientId });
        builder.HasIndex(b => new { b.TenantId, b.Status });
        builder.HasIndex(b => new { b.TenantId, b.AdmissionId });

        builder.HasOne(b => b.Patient)
            .WithMany()
            .HasForeignKey(b => b.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.CreatedBy)
            .WithMany()
            .HasForeignKey(b => b.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Payer)
            .WithMany()
            .HasForeignKey(b => b.PayerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
