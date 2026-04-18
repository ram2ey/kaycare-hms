using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Billing;

public class SaveServiceCatalogItemRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Range(0, 9999999999.99)]
    public decimal UnitPrice { get; set; }

    public bool IsActive { get; set; } = true;
}
