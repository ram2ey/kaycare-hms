using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class BillAdjustmentConfiguration : IEntityTypeConfiguration<BillAdjustment>
{
    public void Configure(EntityTypeBuilder<BillAdjustment> builder)
    {
        builder.HasKey(a => a.BillAdjustmentId);
        builder.Property(a => a.BillAdjustmentId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(a => a.Amount).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(500).IsRequired();

        builder.HasOne(a => a.Bill)
            .WithMany(b => b.Adjustments)
            .HasForeignKey(a => a.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.AdjustedBy)
            .WithMany()
            .HasForeignKey(a => a.AdjustedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.TenantId, a.BillId });
    }
}
