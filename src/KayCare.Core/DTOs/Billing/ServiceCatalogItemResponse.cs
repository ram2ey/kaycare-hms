namespace KayCare.Core.DTOs.Billing;

public class ServiceCatalogItemResponse
{
    public Guid    ServiceCatalogItemId { get; set; }
    public string  Name                 { get; set; } = string.Empty;
    public string? Description          { get; set; }
    public string  Category             { get; set; } = string.Empty;
    public decimal UnitPrice            { get; set; }
    public bool    IsActive             { get; set; }
    public DateTime CreatedAt           { get; set; }
    public DateTime UpdatedAt           { get; set; }
}
