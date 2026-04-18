using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.PatientId);
        builder.Property(p => p.PatientId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(p => p.MedicalRecordNumber).HasMaxLength(50).IsRequired();
        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.MiddleName).HasMaxLength(100);
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Gender).HasMaxLength(20).IsRequired();
        builder.Property(p => p.BloodType).HasMaxLength(5);
        builder.Property(p => p.NationalId).HasMaxLength(50);

        builder.Property(p => p.Email).HasMaxLength(256);
        builder.Property(p => p.PhoneNumber).HasMaxLength(20);
        builder.Property(p => p.AlternatePhone).HasMaxLength(20);

        builder.Property(p => p.AddressLine1).HasMaxLength(200);
        builder.Property(p => p.AddressLine2).HasMaxLength(200);
        builder.Property(p => p.City).HasMaxLength(100);
        builder.Property(p => p.State).HasMaxLength(100);
        builder.Property(p => p.PostalCode).HasMaxLength(20);
        builder.Property(p => p.Country).HasMaxLength(100).HasDefaultValue("GH");

        builder.Property(p => p.EmergencyContactName).HasMaxLength(200);
        builder.Property(p => p.EmergencyContactPhone).HasMaxLength(20);
        builder.Property(p => p.EmergencyContactRelation).HasMaxLength(50);

        builder.Property(p => p.NhisNumber).HasMaxLength(20);
        builder.Property(p => p.InsuranceProvider).HasMaxLength(200);
        builder.Property(p => p.InsurancePolicyNumber).HasMaxLength(100);
        builder.Property(p => p.InsuranceGroupNumber).HasMaxLength(100);

        // CreatedAt from TenantEntity maps to RegisteredAt column in DB
        builder.Property(p => p.CreatedAt)
            .HasColumnName("RegisteredAt")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(p => new { p.TenantId, p.MedicalRecordNumber }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.LastName });

        builder.HasMany(p => p.Allergies)
            .WithOne(a => a.Patient)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
