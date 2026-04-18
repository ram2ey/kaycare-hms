using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class NursingNoteConfiguration : IEntityTypeConfiguration<NursingNote>
{
    public void Configure(EntityTypeBuilder<NursingNote> b)
    {
        b.HasKey(n => n.NursingNoteId);
        b.Property(n => n.NursingNoteId).HasDefaultValueSql("NEWSEQUENTIALID()");

        b.Property(n => n.NoteType).HasMaxLength(50).IsRequired();
        b.Property(n => n.Note).HasMaxLength(4000).IsRequired();

        b.HasIndex(n => new { n.TenantId, n.PatientId, n.CreatedAt });
        b.HasIndex(n => new { n.TenantId, n.AdmissionId });

        b.HasOne(n => n.Patient).WithMany().HasForeignKey(n => n.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(n => n.Author).WithMany().HasForeignKey(n => n.AuthorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(n => n.Admission).WithMany().HasForeignKey(n => n.AdmissionId).OnDelete(DeleteBehavior.Restrict);
    }
}
