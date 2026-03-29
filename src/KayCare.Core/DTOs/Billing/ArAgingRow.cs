namespace KayCare.Core.DTOs.Billing;

public class ArAgingRow
{
    public Guid    BillId              { get; set; }
    public string  BillNumber          { get; set; } = string.Empty;
    public string  PatientName         { get; set; } = string.Empty;
    public string  MedicalRecordNumber { get; set; } = string.Empty;
    public string? PayerName           { get; set; }
    public DateTime IssuedAt           { get; set; }
    public int     DaysOutstanding     { get; set; }
    public string  AgingBucket         { get; set; } = string.Empty;  // "0-30", "31-60", "61-90", "90+"
    public decimal TotalAmount         { get; set; }
    public decimal PaidAmount          { get; set; }
    public decimal BalanceDue          { get; set; }
    public string  Status              { get; set; } = string.Empty;
}

public class ArAgingReport
{
    public decimal TotalBalance0To30   { get; set; }
    public decimal TotalBalance31To60  { get; set; }
    public decimal TotalBalance61To90  { get; set; }
    public decimal TotalBalance90Plus  { get; set; }
    public decimal GrandTotalBalance   { get; set; }
    public List<ArAgingRow> Rows       { get; set; } = [];
}
