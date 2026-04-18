using KayCare.Core.Constants;

namespace KayCare.Core.Entities;

public class Admission : TenantEntity
{
    public Guid    AdmissionId              { get; set; }
    public string  AdmissionNumber          { get; set; } = string.Empty;
    public Guid    PatientId                { get; set; }
    public Guid    BedId                    { get; set; }
    public Guid    WardId                   { get; set; }
    public Guid    AdmittingDoctorUserId    { get; set; }
    public Guid    CreatedByUserId          { get; set; }
    public DateTime AdmissionDate           { get; set; }
    public DateTime? ExpectedDischargeDate  { get; set; }
    public DateTime? ActualDischargeDate    { get; set; }
    public string  Status                   { get; set; } = AdmissionStatus.Active;
    public string  AdmissionReason          { get; set; } = string.Empty;
    public string? DiagnosisOnAdmission     { get; set; }
    public string? DischargeNotes           { get; set; }
    public string? DischargeType            { get; set; }

    // Discharge summary (clinical, filled by doctor)
    public string? FinalDiagnosis           { get; set; }
    public string? TreatmentSummary         { get; set; }
    public string? ProceduresPerformed      { get; set; }
    public string? DischargeMedications     { get; set; }
    public string? DischargeCondition       { get; set; }
    public string? FollowUpInstructions     { get; set; }
    public string? AttendingPhysicianNotes  { get; set; }

    public Patient Patient              { get; set; } = null!;
    public Bed     Bed                  { get; set; } = null!;
    public Ward    Ward                 { get; set; } = null!;
    public User    AdmittingDoctor      { get; set; } = null!;
    public User    CreatedBy            { get; set; } = null!;
    public ICollection<AdmissionTransfer> Transfers { get; set; } = [];
}
