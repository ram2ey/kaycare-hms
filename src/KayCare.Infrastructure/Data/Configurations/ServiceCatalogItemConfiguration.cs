using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class ServiceCatalogItemConfiguration : IEntityTypeConfiguration<ServiceCatalogItem>
{
    public void Configure(EntityTypeBuilder<ServiceCatalogItem> builder)
    {
        builder.HasKey(s => s.ServiceCatalogItemId);
        builder.Property(s => s.ServiceCatalogItemId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.Category).HasMaxLength(100).IsRequired();
        builder.Property(s => s.UnitPrice).HasColumnType("decimal(12,2)");

        builder.Property(s => s.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // Fast lookups when billing staff search the catalog
        builder.HasIndex(s => new { s.TenantId, s.Category });
        builder.HasIndex(s => new { s.TenantId, s.IsActive });
    }
}
