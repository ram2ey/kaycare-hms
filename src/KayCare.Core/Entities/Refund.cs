namespace KayCare.Core.Entities;

public class Refund : TenantEntity
{
    public Guid     RefundId          { get; set; }
    public string   RefundNumber      { get; set; } = string.Empty;  // REF-{YEAR}-{seq}
    public Guid     BillId            { get; set; }
    public Guid     PatientId         { get; set; }
    public Guid?    CreditNoteId      { get; set; }    // optional — refund may stem from a credit note
    public Guid     CreatedByUserId   { get; set; }
    public Guid?    ProcessedByUserId { get; set; }

    public decimal  Amount            { get; set; }
    public string   Reason            { get; set; } = string.Empty;
    public string   RefundMethod      { get; set; } = string.Empty;  // RefundMethod constants
    public string?  Reference         { get; set; }   // cheque #, transfer ref, etc.
    public string   Status            { get; set; } = "Pending";     // RefundStatus constants
    public string?  Notes             { get; set; }
    public DateTime? ProcessedAt      { get; set; }

    public Bill        Bill        { get; set; } = null!;
    public Patient     Patient     { get; set; } = null!;
    public CreditNote? CreditNote  { get; set; }
    public User        CreatedBy   { get; set; } = null!;
    public User?       ProcessedBy { get; set; }
}
