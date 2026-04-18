namespace KayCare.Core.Entities;

public class InpatientCharge
{
    public Guid     InpatientChargeId { get; set; }
    public Guid     TenantId          { get; set; }
    public Guid     AdmissionId       { get; set; }
    public DateOnly ChargeDate        { get; set; }
    public string   Category          { get; set; } = string.Empty;
    public string   Description       { get; set; } = string.Empty;
    public int      Quantity          { get; set; } = 1;
    public decimal  UnitPrice         { get; set; }
    public decimal  TotalPrice        { get; set; }
    public string?  Notes             { get; set; }
    public Guid     CreatedByUserId   { get; set; }
    public DateTime CreatedAt         { get; set; }

    public Admission Admission { get; set; } = null!;
    public User      CreatedBy { get; set; } = null!;
}
