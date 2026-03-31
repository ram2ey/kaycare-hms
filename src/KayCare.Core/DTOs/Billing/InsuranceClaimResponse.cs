namespace KayCare.Core.DTOs.Billing;

public class InsuranceClaimResponse
{
    public Guid      ClaimId          { get; set; }
    public string    ClaimNumber      { get; set; } = string.Empty;
    public Guid      BillId           { get; set; }
    public string    BillNumber       { get; set; } = string.Empty;
    public Guid      PayerId          { get; set; }
    public string    PayerName        { get; set; } = string.Empty;
    public string    PayerType        { get; set; } = string.Empty;
    public Guid      PatientId        { get; set; }
    public string    PatientName      { get; set; } = string.Empty;
    public string    PatientMrn       { get; set; } = string.Empty;
    public string?   NhisNumber       { get; set; }
    public string    Status           { get; set; } = string.Empty;
    public decimal   ClaimAmount      { get; set; }
    public decimal?  ApprovedAmount   { get; set; }
    public string?   RejectionReason  { get; set; }
    public string?   Notes            { get; set; }
    public DateTime? SubmittedAt      { get; set; }
    public DateTime? ResponseAt       { get; set; }
    public Guid?     PaymentId        { get; set; }
    public string    CreatedByName    { get; set; } = string.Empty;
    public DateTime  CreatedAt        { get; set; }
    public DateTime  UpdatedAt        { get; set; }
}
