using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class InsuranceClaimConfiguration : IEntityTypeConfiguration<InsuranceClaim>
{
    public void Configure(EntityTypeBuilder<InsuranceClaim> builder)
    {
        builder.HasKey(c => c.ClaimId);
        builder.Property(c => c.ClaimId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(c => c.ClaimNumber).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Draft");
        builder.Property(c => c.NhisNumber).HasMaxLength(50);
        builder.Property(c => c.RejectionReason).HasMaxLength(1000);
        builder.Property(c => c.Notes).HasMaxLength(1000);

        builder.Property(c => c.ClaimAmount).HasColumnType("decimal(12,2)");
        builder.Property(c => c.ApprovedAmount).HasColumnType("decimal(12,2)");

        builder.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(c => new { c.TenantId, c.ClaimNumber }).IsUnique();
        builder.HasIndex(c => new { c.TenantId, c.Status });
        builder.HasIndex(c => new { c.TenantId, c.PayerId });
        builder.HasIndex(c => new { c.TenantId, c.PatientId });
        builder.HasIndex(c => new { c.TenantId, c.BillId });

        builder.HasOne(c => c.Bill)
            .WithMany()
            .HasForeignKey(c => c.BillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Payer)
            .WithMany()
            .HasForeignKey(c => c.PayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Patient)
            .WithMany()
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CreatedBy)
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Payment)
            .WithMany()
            .HasForeignKey(c => c.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
