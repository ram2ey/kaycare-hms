using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class DispenseEventItemConfiguration : IEntityTypeConfiguration<DispenseEventItem>
{
    public void Configure(EntityTypeBuilder<DispenseEventItem> builder)
    {
        builder.HasKey(i => i.DispenseEventItemId);
        builder.Property(i => i.DispenseEventItemId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.HasOne(i => i.Event)
            .WithMany(e => e.Items)
            .HasForeignKey(i => i.DispenseEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.PrescriptionItem)
            .WithMany()
            .HasForeignKey(i => i.PrescriptionItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
