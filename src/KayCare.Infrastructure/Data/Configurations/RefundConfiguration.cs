using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.HasKey(r => r.RefundId);
        builder.Property(r => r.RefundId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(r => r.RefundNumber).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(r => r.RefundMethod).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Reference).HasMaxLength(200);
        builder.Property(r => r.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Pending");
        builder.Property(r => r.Notes).HasMaxLength(1000);
        builder.Property(r => r.Amount).HasColumnType("decimal(12,2)");

        builder.Property(r => r.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(r => r.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(r => new { r.TenantId, r.RefundNumber }).IsUnique();
        builder.HasIndex(r => new { r.TenantId, r.BillId });
        builder.HasIndex(r => new { r.TenantId, r.Status });

        builder.HasOne(r => r.Bill)
            .WithMany(b => b.Refunds)
            .HasForeignKey(r => r.BillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Patient)
            .WithMany()
            .HasForeignKey(r => r.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CreditNote)
            .WithMany(cn => cn.Refunds)
            .HasForeignKey(r => r.CreditNoteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.CreatedBy)
            .WithMany()
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ProcessedBy)
            .WithMany()
            .HasForeignKey(r => r.ProcessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
