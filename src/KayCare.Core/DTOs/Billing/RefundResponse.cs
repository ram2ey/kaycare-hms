namespace KayCare.Core.DTOs.Billing;

public class RefundResponse
{
    public Guid      RefundId         { get; set; }
    public string    RefundNumber     { get; set; } = string.Empty;
    public Guid      BillId           { get; set; }
    public string    BillNumber       { get; set; } = string.Empty;
    public Guid      PatientId        { get; set; }
    public string    PatientName      { get; set; } = string.Empty;
    public string    PatientMrn       { get; set; } = string.Empty;
    public Guid?     CreditNoteId     { get; set; }
    public string?   CreditNoteNumber { get; set; }
    public decimal   Amount           { get; set; }
    public string    Reason           { get; set; } = string.Empty;
    public string    RefundMethod     { get; set; } = string.Empty;
    public string?   Reference        { get; set; }
    public string    Status           { get; set; } = string.Empty;
    public string?   Notes            { get; set; }
    public string    CreatedByName    { get; set; } = string.Empty;
    public string?   ProcessedByName  { get; set; }
    public DateTime? ProcessedAt      { get; set; }
    public DateTime  CreatedAt        { get; set; }
    public DateTime  UpdatedAt        { get; set; }
}
