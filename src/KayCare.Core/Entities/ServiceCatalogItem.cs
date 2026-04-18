namespace KayCare.Core.Entities;

/// <summary>
/// Tenant-scoped fee schedule — configurable list of services and their standard prices.
/// Admin/SuperAdmin manage this; billing staff use it to auto-fill bill line items.
/// </summary>
public class ServiceCatalogItem : TenantEntity
{
    public Guid    ServiceCatalogItemId { get; set; }
    public string  Name                 { get; set; } = string.Empty; // e.g. "General Consultation"
    public string? Description          { get; set; }
    public string  Category             { get; set; } = string.Empty; // matches BillItem categories
    public decimal UnitPrice            { get; set; }
    public bool    IsActive             { get; set; } = true;
}
