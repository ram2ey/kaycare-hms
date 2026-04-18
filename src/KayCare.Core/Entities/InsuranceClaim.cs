namespace KayCare.Core.Entities;

public class InsuranceClaim : TenantEntity
{
    public Guid     ClaimId           { get; set; }
    public string   ClaimNumber       { get; set; } = string.Empty;  // CLM-{YEAR}-{seq}
    public Guid     BillId            { get; set; }
    public Guid     PayerId           { get; set; }
    public Guid     PatientId         { get; set; }
    public Guid     CreatedByUserId   { get; set; }

    /// <summary>NHIS or policy number snapshotted from the patient at claim creation time.</summary>
    public string?  NhisNumber        { get; set; }

    public string   Status            { get; set; } = "Draft";       // ClaimStatus constants
    public decimal  ClaimAmount       { get; set; }
    public decimal? ApprovedAmount    { get; set; }
    public string?  RejectionReason   { get; set; }
    public string?  Notes             { get; set; }
    public DateTime? SubmittedAt      { get; set; }
    public DateTime? ResponseAt       { get; set; }

    /// <summary>Payment created on the bill when claim is approved.</summary>
    public Guid?    PaymentId         { get; set; }

    public Bill     Bill              { get; set; } = null!;
    public Payer    Payer             { get; set; } = null!;
    public Patient  Patient           { get; set; } = null!;
    public User     CreatedBy         { get; set; } = null!;
    public Payment? Payment           { get; set; }
}
