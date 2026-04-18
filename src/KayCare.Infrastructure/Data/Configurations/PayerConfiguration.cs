using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class PayerConfiguration : IEntityTypeConfiguration<Payer>
{
    public void Configure(EntityTypeBuilder<Payer> builder)
    {
        builder.HasKey(p => p.PayerId);
        builder.Property(p => p.PayerId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Type).HasMaxLength(50).IsRequired();
        builder.Property(p => p.ContactPhone).HasMaxLength(30);
        builder.Property(p => p.ContactEmail).HasMaxLength(256);
        builder.Property(p => p.Notes).HasMaxLength(1000);

        builder.Property(p => p.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(p => new { p.TenantId, p.Name }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.Type });
    }
}
