using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class RadiologyOrderConfiguration : IEntityTypeConfiguration<RadiologyOrder>
{
    public void Configure(EntityTypeBuilder<RadiologyOrder> builder)
    {
        builder.HasKey(o => o.RadiologyOrderId);
        builder.Property(o => o.RadiologyOrderId).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(o => o.Priority).HasMaxLength(20).IsRequired();
        builder.Property(o => o.Status).HasMaxLength(30).IsRequired();
        builder.Property(o => o.ClinicalIndication).HasMaxLength(1000);
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(o => o.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(o => new { o.TenantId, o.PatientId });
        builder.HasIndex(o => new { o.TenantId, o.Status });
        builder.HasIndex(o => new { o.TenantId, o.CreatedAt });

        builder.HasOne(o => o.Patient).WithMany().HasForeignKey(o => o.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Bill).WithMany().HasForeignKey(o => o.BillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.OrderingDoctor).WithMany().HasForeignKey(o => o.OrderingDoctorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(o => o.Items).WithOne(i => i.RadiologyOrder).HasForeignKey(i => i.RadiologyOrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
