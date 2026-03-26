using MediCloud.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCloud.Infrastructure.Data.Configurations;

public class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
{
    public void Configure(EntityTypeBuilder<Consultation> builder)
    {
        builder.HasKey(c => c.ConsultationId);
        builder.Property(c => c.ConsultationId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(c => c.SubjectiveNotes).HasColumnType("nvarchar(max)");
        builder.Property(c => c.ObjectiveNotes).HasColumnType("nvarchar(max)");
        builder.Property(c => c.AssessmentNotes).HasColumnType("nvarchar(max)");
        builder.Property(c => c.PlanNotes).HasColumnType("nvarchar(max)");
        builder.Property(c => c.SecondaryDiagnoses).HasColumnType("nvarchar(max)").HasDefaultValue("[]");

        builder.Property(c => c.TemperatureCelsius).HasColumnType("decimal(4,1)");
        builder.Property(c => c.WeightKg).HasColumnType("decimal(5,2)");
        builder.Property(c => c.HeightCm).HasColumnType("decimal(5,1)");
        builder.Property(c => c.OxygenSaturationPct).HasColumnType("decimal(4,1)");

        builder.Property(c => c.PrimaryDiagnosisCode).HasMaxLength(20);
        builder.Property(c => c.PrimaryDiagnosisDesc).HasMaxLength(500);
        builder.Property(c => c.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Draft");

        builder.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // One consultation per appointment
        builder.HasIndex(c => c.AppointmentId).IsUnique();
        builder.HasIndex(c => new { c.TenantId, c.PatientId });

        builder.HasOne(c => c.Appointment)
            .WithMany()
            .HasForeignKey(c => c.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Patient)
            .WithMany()
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Doctor)
            .WithMany()
            .HasForeignKey(c => c.DoctorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
