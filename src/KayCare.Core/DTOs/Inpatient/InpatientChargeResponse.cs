namespace KayCare.Core.DTOs.Inpatient;

public class InpatientChargeResponse
{
    public Guid     InpatientChargeId { get; set; }
    public Guid     AdmissionId       { get; set; }
    public string   ChargeDate        { get; set; } = string.Empty;
    public string   Category          { get; set; } = string.Empty;
    public string   Description       { get; set; } = string.Empty;
    public int      Quantity          { get; set; }
    public decimal  UnitPrice         { get; set; }
    public decimal  TotalPrice        { get; set; }
    public string?  Notes             { get; set; }
    public string   CreatedByName     { get; set; } = string.Empty;
    public DateTime CreatedAt         { get; set; }
}
