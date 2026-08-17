using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Data;

/// <summary>
/// Resolves what a service should actually be billed at: the payer's negotiated tariff when one
/// exists, otherwise the standard service catalog price. Used by charge capture and by bill
/// creation, so a price is never taken solely from client-submitted or unqualified catalog input.
/// </summary>
public static class TariffPricingExtensions
{
    /// <summary>
    /// Single-lookup variant: finds the matching ServiceCatalogItem (by name, then category
    /// fallback), then resolves its price via <see cref="ResolveTariffOrCatalogPriceAsync"/>.
    /// </summary>
    public static async Task<decimal> ResolvePriceAsync(this AppDbContext db, string name, string category, Guid? payerId, CancellationToken ct)
    {
        var item = await db.ServiceCatalogItems
            .FirstOrDefaultAsync(s => s.IsActive && (s.Name.ToLower() == name.ToLower() || s.Category.ToLower() == category.ToLower()), ct);

        return await ResolveTariffOrCatalogPriceAsync(db, item, payerId, ct);
    }

    /// <summary>
    /// Same resolution as <see cref="ResolvePriceAsync"/>, but matches against a pre-loaded
    /// catalog list to avoid N+1 queries when pricing many items in a loop.
    /// </summary>
    public static async Task<decimal> ResolvePriceFromCatalogAsync(this AppDbContext db, List<ServiceCatalogItem> catalog, string name, string category, Guid? payerId, CancellationToken ct)
    {
        var item = catalog.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? catalog.FirstOrDefault(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        return await ResolveTariffOrCatalogPriceAsync(db, item, payerId, ct);
    }

    /// <summary>Same resolution, taking an already-resolved catalog item directly.</summary>
    public static async Task<decimal> ResolveTariffOrCatalogPriceAsync(this AppDbContext db, ServiceCatalogItem? item, Guid? payerId, CancellationToken ct)
    {
        if (item is null) return 0m;

        if (payerId.HasValue)
        {
            var tariff = await db.PayerTariffs
                .FirstOrDefaultAsync(t => t.PayerId == payerId.Value
                                        && t.ServiceCatalogItemId == item.ServiceCatalogItemId
                                        && t.IsActive, ct);
            if (tariff is not null) return tariff.TariffPrice;
        }

        return item.UnitPrice;
    }
}
