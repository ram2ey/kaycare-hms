using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.TenantId);
        builder.Property(t => t.TenantId).HasDefaultValueSql("gen_random_uuid()");

        builder.ToTable(t => t.HasCheckConstraint("CK_Tenants_Quotas_Positive",
            "\"MaxUsers\" > 0 AND \"StorageQuotaGB\" > 0 AND \"AiMonthlyQuota\" >= 0 AND \"AiRequestsThisMonth\" >= 0"));
        builder.Property(t => t.TenantCode).HasMaxLength(50).IsRequired();
        builder.Property(t => t.TenantName).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Subdomain).HasMaxLength(100).IsRequired();
        builder.Property(t => t.SubscriptionPlan).HasMaxLength(50).HasDefaultValue("standard");
        builder.Property(t => t.IsAiEnabled).HasDefaultValue(true);
        builder.Property(t => t.AiMonthlyQuota).HasDefaultValue(500);
        builder.Property(t => t.AiRequestsThisMonth).HasDefaultValue(0);
        builder.Property(t => t.AllowedAiTiers).HasMaxLength(200).HasDefaultValue("Standard");
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(t => t.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(t => t.TenantCode).IsUnique();
        builder.HasIndex(t => t.Subdomain).IsUnique();
    }
}
