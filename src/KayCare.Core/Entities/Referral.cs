namespace KayCare.Core.Entities;

public class Referral : TenantEntity
{
    public Guid  ReferralId              { get; set; }
    public string ReferralNumber         { get; set; } = string.Empty; // REF-YYYY-NNNNN
    public Guid  PatientId               { get; set; }
    public Guid? ConsultationId          { get; set; }
    public Guid  ReferringDoctorUserId   { get; set; }
    public Guid? ReferredToDoctorUserId  { get; set; }
    public string? ReferredToDepartment  { get; set; }
    public string ReferralType           { get; set; } = "Internal"; // Internal|External
    public string? ExternalFacility      { get; set; }
    public string Urgency                { get; set; } = "Routine";  // Routine|Urgent|Emergency
    public string Reason                 { get; set; } = string.Empty;
    public string? ClinicalNotes         { get; set; }
    public string Status                 { get; set; } = "Draft";    // Draft|Sent|Accepted|Completed|Declined|Cancelled
    public string? ResponseNotes         { get; set; }

    // Navigation
    public Patient          Patient          { get; set; } = null!;
    public Consultation?    Consultation     { get; set; }
    public User             ReferringDoctor  { get; set; } = null!;
    public User?            ReferredToDoctor { get; set; }
}
