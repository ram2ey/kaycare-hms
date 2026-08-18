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
        // BloodType is encrypted (DB17) - unbounded "text" column since ciphertext is longer than
        // plaintext (base64 of nonce+ciphertext+tag); a fixed MaxLength here would risk truncation.
        builder.Property(p => p.NationalId).HasMaxLength(50);

        // Email is encrypted (DB17) - unbounded, see BloodType note above.
        builder.Property(p => p.PhoneNumber).HasMaxLength(20);
        builder.Property(p => p.AlternatePhone).HasMaxLength(20);

        // AddressLine1/2, City, State, PostalCode are encrypted (DB17) - unbounded, see above.
        builder.Property(p => p.Country).HasMaxLength(100).HasDefaultValue("GH");

        // EmergencyContactName/Phone/Relation are encrypted (DB17) - unbounded, see above.

        // NhisNumber, InsurancePolicyNumber, InsuranceGroupNumber are encrypted (DB18) - unbounded,
        // see above. InsuranceProvider (the payer/insurer name, e.g. "NHIS") stays plaintext - a
        // shared category value across many patients, not personally identifying on its own.
        builder.Property(p => p.InsuranceProvider).HasMaxLength(200);

        // CreatedAt from TenantEntity maps to RegisteredAt column in DB
        builder.Property(p => p.CreatedAt)
            .HasColumnName("RegisteredAt")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(p => new { p.TenantId, p.MedicalRecordNumber }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.LastName });

        // Partial (filtered) unique index: NationalId has no uniqueness guarantee at all today,
        // so two Patient rows in the same tenant could carry the same national ID with nothing
        // to catch it. Filtered on NOT NULL since it's an optional field — most patients won't
        // have one recorded, and NULL <> NULL means a plain unique index would incorrectly
        // reject the second NULL-NationalId patient rather than allowing any number of them.
        builder.HasIndex(p => new { p.TenantId, p.NationalId })
            .IsUnique()
            .HasFilter("\"NationalId\" IS NOT NULL");

        builder.HasMany(p => p.Allergies)
            .WithOne(a => a.Patient)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
