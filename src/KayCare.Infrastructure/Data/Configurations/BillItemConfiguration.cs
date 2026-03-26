using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class BillItemConfiguration : IEntityTypeConfiguration<BillItem>
{
    public void Configure(EntityTypeBuilder<BillItem> builder)
    {
        builder.HasKey(i => i.ItemId);
        builder.Property(i => i.ItemId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(i => i.Description).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Category).HasMaxLength(100);

        builder.Property(i => i.UnitPrice).HasColumnType("decimal(12,2)");
        builder.Property(i => i.TotalPrice)
            .HasColumnType("decimal(12,2)")
            .HasComputedColumnSql("[Quantity] * [UnitPrice]", stored: true);

        builder.HasOne(i => i.Bill)
            .WithMany(b => b.Items)
            .HasForeignKey(i => i.BillId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
