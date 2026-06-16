namespace KayCare.Core.DTOs.Billing;

public class PayerTariffResponse
{
    public Guid    PayerTariffId        { get; set; }
    public Guid    PayerId              { get; set; }
    public string  PayerName            { get; set; } = string.Empty;
    public Guid    ServiceCatalogItemId { get; set; }
    public string  ServiceName          { get; set; } = string.Empty;
    public string  ServiceCategory      { get; set; } = string.Empty;
    public decimal StandardPrice        { get; set; }
    public string? TariffCode           { get; set; }
    public decimal TariffPrice          { get; set; }
    public DateOnly EffectiveDate       { get; set; }
    public bool    IsActive             { get; set; }
    public DateTime CreatedAt           { get; set; }
    public DateTime UpdatedAt           { get; set; }
}
