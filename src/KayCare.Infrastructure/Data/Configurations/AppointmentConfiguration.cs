using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(a => a.AppointmentId);
        builder.Property(a => a.AppointmentId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(a => a.AppointmentType).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Scheduled");
        builder.Property(a => a.ChiefComplaint).HasMaxLength(1000);
        builder.Property(a => a.Room).HasMaxLength(50);
        builder.Property(a => a.Notes).HasMaxLength(2000);
        builder.Property(a => a.CancellationReason).HasMaxLength(500);

        builder.Property(a => a.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(a => a.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(a => new { a.TenantId, a.DoctorUserId, a.ScheduledAt });
        builder.HasIndex(a => new { a.TenantId, a.PatientId });

        builder.HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
