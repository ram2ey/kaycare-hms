namespace KayCare.Core.DTOs.Billing;

public class BillResponse
{
    public Guid      BillId              { get; set; }
    public string    BillNumber          { get; set; } = string.Empty;
    public Guid      PatientId           { get; set; }
    public string    PatientName         { get; set; } = string.Empty;
    public string    MedicalRecordNumber { get; set; } = string.Empty;
    public string    Status              { get; set; } = string.Empty;
    public decimal   TotalAmount         { get; set; }
    public decimal   AdjustmentTotal     { get; set; }
    public decimal   DiscountAmount      { get; set; }
    public decimal   WriteOffAmount      { get; set; }
    public decimal   PaidAmount          { get; set; }
    public decimal   BalanceDue          { get; set; }
    public DateTime? IssuedAt            { get; set; }
    public DateTime  CreatedAt           { get; set; }
}
