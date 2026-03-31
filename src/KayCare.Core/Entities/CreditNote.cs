namespace KayCare.Core.Entities;

public class CreditNote : TenantEntity
{
    public Guid     CreditNoteId      { get; set; }
    public string   CreditNoteNumber  { get; set; } = string.Empty;  // CN-{YEAR}-{seq}
    public Guid     BillId            { get; set; }
    public Guid     PatientId         { get; set; }
    public Guid     CreatedByUserId   { get; set; }
    public Guid?    ApprovedByUserId  { get; set; }

    public decimal  Amount            { get; set; }
    public string   Reason            { get; set; } = string.Empty;
    public string   Status            { get; set; } = "Draft";       // CreditNoteStatus constants
    public string?  Notes             { get; set; }
    public DateTime? ApprovedAt       { get; set; }
    public DateTime? AppliedAt        { get; set; }

    public Bill     Bill              { get; set; } = null!;
    public Patient  Patient           { get; set; } = null!;
    public User     CreatedBy         { get; set; } = null!;
    public User?    ApprovedBy        { get; set; }

    public ICollection<Refund> Refunds { get; set; } = [];
}
