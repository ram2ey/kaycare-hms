namespace KayCare.Core.Entities;

/// <summary>
/// Payer-specific price override for a service catalog item.
/// e.g. NHIA pays GHS 12.50 (code GHA-LAB-001) for a Full Blood Count,
/// while the standard price is GHS 20.00.
/// </summary>
public class PayerTariff : TenantEntity
{
    public Guid    PayerTariffId       { get; set; }
    public Guid    PayerId             { get; set; }
    public Guid    ServiceCatalogItemId { get; set; }

    /// <summary>Payer-specific tariff code (e.g. NHIS code "GHA-LAB-001").</summary>
    public string? TariffCode          { get; set; }

    /// <summary>The price this payer pays for the service.</summary>
    public decimal TariffPrice         { get; set; }

    public DateOnly EffectiveDate      { get; set; }
    public bool    IsActive            { get; set; } = true;

    public Payer              Payer              { get; set; } = null!;
    public ServiceCatalogItem ServiceCatalogItem { get; set; } = null!;
}
