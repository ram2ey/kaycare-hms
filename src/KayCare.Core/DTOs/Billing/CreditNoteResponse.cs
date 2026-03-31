namespace KayCare.Core.DTOs.Billing;

public class CreditNoteResponse
{
    public Guid      CreditNoteId     { get; set; }
    public string    CreditNoteNumber { get; set; } = string.Empty;
    public Guid      BillId           { get; set; }
    public string    BillNumber       { get; set; } = string.Empty;
    public Guid      PatientId        { get; set; }
    public string    PatientName      { get; set; } = string.Empty;
    public string    PatientMrn       { get; set; } = string.Empty;
    public decimal   Amount           { get; set; }
    public string    Reason           { get; set; } = string.Empty;
    public string    Status           { get; set; } = string.Empty;
    public string?   Notes            { get; set; }
    public string    CreatedByName    { get; set; } = string.Empty;
    public string?   ApprovedByName   { get; set; }
    public DateTime? ApprovedAt       { get; set; }
    public DateTime? AppliedAt        { get; set; }
    public DateTime  CreatedAt        { get; set; }
    public DateTime  UpdatedAt        { get; set; }
}
