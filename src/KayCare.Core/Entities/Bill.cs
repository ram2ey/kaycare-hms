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
    public decimal  DiscountAmount    { get; set; }   // default 0
    public string?  DiscountReason    { get; set; }
    public decimal  PaidAmount        { get; set; }
    public decimal  BalanceDue        { get; set; }   // computed: TotalAmount - DiscountAmount - PaidAmount
    public DateTime? IssuedAt         { get; set; }

    public Patient Patient   { get; set; } = null!;
    public User    CreatedBy { get; set; } = null!;
    public Payer?  Payer     { get; set; }

    public ICollection<BillItem> Items    { get; set; } = [];
    public ICollection<Payment>  Payments { get; set; } = [];
}
