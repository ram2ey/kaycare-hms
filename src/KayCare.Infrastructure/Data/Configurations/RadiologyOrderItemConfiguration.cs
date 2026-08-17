using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class RadiologyOrderItemConfiguration : IEntityTypeConfiguration<RadiologyOrderItem>
{
    public void Configure(EntityTypeBuilder<RadiologyOrderItem> builder)
    {
        builder.HasKey(i => i.RadiologyOrderItemId);
        builder.Property(i => i.RadiologyOrderItemId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.ToTable(t => t.HasCheckConstraint("CK_RadiologyOrderItems_Status",
            "\"Status\" IN ('Ordered','Acquired','Reported','Signed')"));
        builder.Property(i => i.ProcedureName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Modality).HasMaxLength(20).IsRequired();
        builder.Property(i => i.BodyPart).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Department).HasMaxLength(100).IsRequired();
        builder.Property(i => i.AccessionNumber).HasMaxLength(30);
        builder.Property(i => i.Status).HasMaxLength(30).IsRequired();
        builder.Property(i => i.Findings).HasMaxLength(4000);
        builder.Property(i => i.Impression).HasMaxLength(2000);
        builder.Property(i => i.Recommendations).HasMaxLength(1000);
        builder.Property(i => i.PacsStudyUid).HasMaxLength(200);
        builder.Property(i => i.PacsViewerUrl).HasMaxLength(500);

        builder.HasIndex(i => new { i.TenantId, i.RadiologyOrderId });
        builder.HasIndex(i => new { i.TenantId, i.AccessionNumber });

        builder.HasOne(i => i.ImagingProcedure).WithMany().HasForeignKey(i => i.ImagingProcedureId).OnDelete(DeleteBehavior.Restrict);
    }
}
