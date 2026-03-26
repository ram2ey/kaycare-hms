using MediCloud.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCloud.Infrastructure.Data.Configurations;

public class PatientAllergyConfiguration : IEntityTypeConfiguration<PatientAllergy>
{
    public void Configure(EntityTypeBuilder<PatientAllergy> builder)
    {
        builder.HasKey(a => a.AllergyId);
        builder.Property(a => a.AllergyId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(a => a.AllergyType).HasMaxLength(50).IsRequired();
        builder.Property(a => a.AllergenName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Reaction).HasMaxLength(500);
        builder.Property(a => a.Severity).HasMaxLength(20).IsRequired();
        builder.Property(a => a.RecordedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(a => new { a.TenantId, a.PatientId });
    }
}
