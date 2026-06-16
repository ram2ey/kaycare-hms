using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class CriticalCallLogConfiguration : IEntityTypeConfiguration<CriticalCallLog>
{
    public void Configure(EntityTypeBuilder<CriticalCallLog> builder)
    {
        builder.HasKey(c => c.CriticalCallLogId);
        builder.Property(c => c.CriticalCallLogId).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(c => c.RecipientName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.CalledByName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Notes).HasMaxLength(1000);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(c => new { c.TenantId, c.LabOrderItemId }).IsUnique();
    }
}
