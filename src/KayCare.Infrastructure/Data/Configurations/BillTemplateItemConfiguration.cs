using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class BillTemplateItemConfiguration : IEntityTypeConfiguration<BillTemplateItem>
{
    public void Configure(EntityTypeBuilder<BillTemplateItem> builder)
    {
        builder.HasKey(i => i.BillTemplateItemId);
        builder.Property(i => i.BillTemplateItemId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_BillTemplateItems_Quantity_Positive", "\"Quantity\" > 0");
            t.HasCheckConstraint("CK_BillTemplateItems_UnitPrice_NonNegative", "\"UnitPrice\" >= 0");
        });

        builder.Property(i => i.Description).HasMaxLength(500).IsRequired();
        builder.Property(i => i.Category).HasMaxLength(100);

        builder.Property(i => i.UnitPrice).HasColumnType("decimal(12,2)");
        builder.Property(i => i.TotalPrice)
            .HasColumnType("decimal(12,2)")
            .HasComputedColumnSql("[Quantity] * [UnitPrice]", stored: true);

        builder.HasOne(i => i.BillTemplate)
            .WithMany(t => t.Items)
            .HasForeignKey(i => i.BillTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.TenantId, i.BillTemplateId });
    }
}
