using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class PayerTariffConfiguration : IEntityTypeConfiguration<PayerTariff>
{
    public void Configure(EntityTypeBuilder<PayerTariff> builder)
    {
        builder.ToTable("PayerTariffs");
        builder.HasKey(t => t.PayerTariffId);

        builder.Property(t => t.TariffCode).HasMaxLength(100);
        builder.Property(t => t.TariffPrice).HasPrecision(18, 2);

        // Unique per tenant: one tariff per payer+service combination
        builder.HasIndex(t => new { t.TenantId, t.PayerId, t.ServiceCatalogItemId })
               .IsUnique();

        builder.HasOne(t => t.Payer)
               .WithMany()
               .HasForeignKey(t => t.PayerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.ServiceCatalogItem)
               .WithMany()
               .HasForeignKey(t => t.ServiceCatalogItemId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
