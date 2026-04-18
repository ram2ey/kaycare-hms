namespace KayCare.Core.Entities;

public class BillItem
{
    public Guid     ItemId      { get; set; }
    public Guid     TenantId    { get; set; }
    public Guid     BillId      { get; set; }
    public string   Description { get; set; } = string.Empty;
    public string?  Category    { get; set; }
    public int      Quantity    { get; set; }
    public decimal  UnitPrice   { get; set; }
    public decimal  TotalPrice  { get; set; }   // computed: Quantity * UnitPrice

    /// <summary>Identifies the clinical event that auto-generated this charge (null = manual entry).</summary>
    public string? SourceType { get; set; }  // "Consultation" | "LabOrder" | "Prescription"
    public Guid?   SourceId   { get; set; }  // ConsultationId | LabOrderItemId | DispenseEventItemId

    public Bill Bill { get; set; } = null!;
}
