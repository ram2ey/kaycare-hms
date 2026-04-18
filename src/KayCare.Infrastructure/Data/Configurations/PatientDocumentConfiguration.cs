using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class PatientDocumentConfiguration : IEntityTypeConfiguration<PatientDocument>
{
    public void Configure(EntityTypeBuilder<PatientDocument> builder)
    {
        builder.HasKey(d => d.DocumentId);
        builder.Property(d => d.DocumentId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(d => d.FileName).HasMaxLength(260).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Category).HasMaxLength(50).IsRequired().HasDefaultValue("Other");
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.Property(d => d.BlobPath).HasMaxLength(500).IsRequired();
        builder.Property(d => d.ContainerName).HasMaxLength(63).IsRequired();

        builder.Property(d => d.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(d => d.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(d => new { d.TenantId, d.PatientId });
        builder.HasIndex(d => new { d.TenantId, d.Category });

        builder.HasOne(d => d.Patient)
            .WithMany()
            .HasForeignKey(d => d.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.UploadedBy)
            .WithMany()
            .HasForeignKey(d => d.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
