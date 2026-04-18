using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class PrescriptionTemplateItemConfiguration : IEntityTypeConfiguration<PrescriptionTemplateItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionTemplateItem> builder)
    {
        builder.HasKey(i => i.TemplateItemId);
        builder.Property(i => i.TemplateItemId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(i => i.MedicationName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.GenericName).HasMaxLength(200);
        builder.Property(i => i.Strength).HasMaxLength(100).IsRequired();
        builder.Property(i => i.DosageForm).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Frequency).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Instructions).HasMaxLength(500);

        builder.HasOne(i => i.Template)
            .WithMany(t => t.Items)
            .HasForeignKey(i => i.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
