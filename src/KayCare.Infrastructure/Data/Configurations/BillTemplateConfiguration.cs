using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class BillTemplateConfiguration : IEntityTypeConfiguration<BillTemplate>
{
    public void Configure(EntityTypeBuilder<BillTemplate> builder)
    {
        builder.HasKey(t => t.BillTemplateId);
        builder.Property(t => t.BillTemplateId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.Category).HasMaxLength(100);

        builder.Property(t => t.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(t => t.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(t => new { t.TenantId, t.Name }).IsUnique();
        builder.HasIndex(t => new { t.TenantId, t.IsActive });
    }
}
