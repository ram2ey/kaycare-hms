using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class IcdCodeConfiguration : IEntityTypeConfiguration<IcdCode>
{
    public void Configure(EntityTypeBuilder<IcdCode> builder)
    {
        builder.HasKey(c => c.IcdCodeId);
        builder.Property(c => c.IcdCodeId).UseIdentityColumn();

        builder.Property(c => c.Code).HasMaxLength(10).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500).IsRequired();
        builder.Property(c => c.Chapter).HasMaxLength(200).IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.Description);
    }
}
