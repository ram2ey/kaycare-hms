using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class AdmissionTransferConfiguration : IEntityTypeConfiguration<AdmissionTransfer>
{
    public void Configure(EntityTypeBuilder<AdmissionTransfer> builder)
    {
        builder.HasKey(t => t.AdmissionTransferId);
        builder.Property(t => t.AdmissionTransferId).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(t => t.Reason).HasMaxLength(500);

        builder.HasOne(t => t.FromBed).WithMany().HasForeignKey(t => t.FromBedId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.FromWard).WithMany().HasForeignKey(t => t.FromWardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.ToBed).WithMany().HasForeignKey(t => t.ToBedId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.ToWard).WithMany().HasForeignKey(t => t.ToWardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.TransferredBy).WithMany().HasForeignKey(t => t.TransferredByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
