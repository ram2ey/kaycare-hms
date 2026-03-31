namespace KayCare.Core.Entities;

public class Bill : TenantEntity
{
    public Guid     BillId            { get; set; }
    public string   BillNumber        { get; set; } = string.Empty;
    public Guid     PatientId         { get; set; }
    public Guid?    ConsultationId    { get; set; }
    public Guid?    PayerId           { get; set; }   // nullable FK → Payer
    public Guid     CreatedByUserId   { get; set; }
    public string   Status            { get; set; } = "Draft";
    public string?  Notes             { get; set; }
    public decimal  TotalAmount       { get; set; }
    public decimal  AdjustmentTotal  { get; set; }   // denormalized sum of BillAdjustments
    public decimal  DiscountAmount   { get; set; }   // default 0
    public string?  DiscountReason   { get; set; }
    public decimal  WriteOffAmount   { get; set; }   // bad-debt write-off
    public string?  WriteOffReason   { get; set; }
    public decimal  CreditNoteTotal  { get; set; }   // denormalized sum of applied credit notes
    public decimal  PaidAmount       { get; set; }
    public decimal  BalanceDue       { get; set; }   // computed: TotalAmount + AdjustmentTotal - DiscountAmount - WriteOffAmount - CreditNoteTotal - PaidAmount
    public DateTime? IssuedAt         { get; set; }

    public Patient Patient   { get; set; } = null!;
    public User    CreatedBy { get; set; } = null!;
    public Payer?  Payer     { get; set; }

    public ICollection<BillItem>       Items       { get; set; } = [];
    public ICollection<Payment>        Payments    { get; set; } = [];
    public ICollection<BillAdjustment> Adjustments { get; set; } = [];
    public ICollection<CreditNote>     CreditNotes { get; set; } = [];
    public ICollection<Refund>         Refunds     { get; set; } = [];
}
