using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class PrescriptionTemplateConfiguration : IEntityTypeConfiguration<PrescriptionTemplate>
{
    public void Configure(EntityTypeBuilder<PrescriptionTemplate> builder)
    {
        builder.HasKey(t => t.TemplateId);
        builder.Property(t => t.TemplateId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);

        builder.Property(t => t.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(t => t.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(t => new { t.TenantId, t.CreatedByUserId });
        builder.HasIndex(t => new { t.TenantId, t.IsShared });

        builder.HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
