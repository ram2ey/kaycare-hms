using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class PayerTariffConfiguration : IEntityTypeConfiguration<PayerTariff>
{
    public void Configure(EntityTypeBuilder<PayerTariff> builder)
    {
        builder.ToTable("PayerTariffs", t => t.HasCheckConstraint("CK_PayerTariffs_TariffPrice_NonNegative", "\"TariffPrice\" >= 0"));
        builder.HasKey(t => t.PayerTariffId);

        builder.Property(t => t.TariffCode).HasMaxLength(100);
        // decimal(12,2) matches every other money column in the schema (Bills, BillItems, Wards,
        // etc.) — this was the one outlier still on the wider EF-default-style HasPrecision(18,2).
        builder.Property(t => t.TariffPrice).HasColumnType("decimal(12,2)");

        // Unique per tenant: one tariff per payer+service combination
        builder.HasIndex(t => new { t.TenantId, t.PayerId, t.ServiceCatalogItemId })
               .IsUnique();

        // Both were Cascade — deleting a Payer or ServiceCatalogItem would silently wipe out
        // negotiated pricing history. Restrict matches the DB2/DB3 pattern established earlier
        // (financial/pricing records must block deletion of a parent they still reference, not
        // disappear with it).
        builder.HasOne(t => t.Payer)
               .WithMany()
               .HasForeignKey(t => t.PayerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ServiceCatalogItem)
               .WithMany()
               .HasForeignKey(t => t.ServiceCatalogItemId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
