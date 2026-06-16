using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Billing;

public class SavePayerTariffRequest
{
    [Required]
    public Guid PayerId { get; set; }

    [Required]
    public Guid ServiceCatalogItemId { get; set; }

    [MaxLength(100)]
    public string? TariffCode { get; set; }

    [Required, Range(0, 9999999999.99)]
    public decimal TariffPrice { get; set; }

    public DateOnly EffectiveDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public bool IsActive { get; set; } = true;
}
