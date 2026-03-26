using MediCloud.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCloud.Infrastructure.Data.Configurations;

public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.HasKey(i => i.ItemId);
        builder.Property(i => i.ItemId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(i => i.MedicationName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.GenericName).HasMaxLength(200);
        builder.Property(i => i.Strength).HasMaxLength(100).IsRequired();
        builder.Property(i => i.DosageForm).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Frequency).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Instructions).HasMaxLength(500);

        builder.HasOne(i => i.Prescription)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
