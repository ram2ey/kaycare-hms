using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
{
    public void Configure(EntityTypeBuilder<CreditNote> builder)
    {
        builder.HasKey(c => c.CreditNoteId);
        builder.Property(c => c.CreditNoteId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(c => c.CreditNoteNumber).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(c => c.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Draft");
        builder.Property(c => c.Notes).HasMaxLength(1000);
        builder.Property(c => c.Amount).HasColumnType("decimal(12,2)");

        builder.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(c => new { c.TenantId, c.CreditNoteNumber }).IsUnique();
        builder.HasIndex(c => new { c.TenantId, c.BillId });
        builder.HasIndex(c => new { c.TenantId, c.Status });

        builder.HasOne(c => c.Bill)
            .WithMany(b => b.CreditNotes)
            .HasForeignKey(c => c.BillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Patient)
            .WithMany()
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CreatedBy)
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ApprovedBy)
            .WithMany()
            .HasForeignKey(c => c.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
