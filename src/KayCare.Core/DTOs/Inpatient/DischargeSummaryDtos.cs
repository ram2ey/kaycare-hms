namespace KayCare.Core.DTOs.Inpatient;

public class UpdateDischargeSummaryRequest
{
    public string? FinalDiagnosis          { get; set; }
    public string? TreatmentSummary        { get; set; }
    public string? ProceduresPerformed     { get; set; }
    public string? DischargeMedications    { get; set; }
    public string? DischargeCondition      { get; set; }
    public string? FollowUpInstructions    { get; set; }
    public string? AttendingPhysicianNotes { get; set; }
}

public class DischargeSummaryResponse
{
    public Guid    AdmissionId            { get; set; }
    public string  AdmissionNumber        { get; set; } = string.Empty;

    // Patient
    public Guid    PatientId              { get; set; }
    public string  PatientName            { get; set; } = string.Empty;
    public string  PatientMrn             { get; set; } = string.Empty;
    public string? PatientDateOfBirth     { get; set; }
    public string? PatientGender          { get; set; }
    public string? PatientPhone           { get; set; }

    // Admission details
    public string  WardName               { get; set; } = string.Empty;
    public string  WardType               { get; set; } = string.Empty;
    public string  BedNumber              { get; set; } = string.Empty;
    public string  AdmittingDoctorName    { get; set; } = string.Empty;
    public DateTime AdmissionDate         { get; set; }
    public DateTime? ActualDischargeDate  { get; set; }
    public int?    LengthOfStayDays       { get; set; }
    public string  Status                 { get; set; } = string.Empty;
    public string  AdmissionReason        { get; set; } = string.Empty;
    public string? DiagnosisOnAdmission   { get; set; }
    public string? DischargeType          { get; set; }
    public string? DischargeNotes         { get; set; }

    // Clinical summary (editable)
    public string? FinalDiagnosis          { get; set; }
    public string? TreatmentSummary        { get; set; }
    public string? ProceduresPerformed     { get; set; }
    public string? DischargeMedications    { get; set; }
    public string? DischargeCondition      { get; set; }
    public string? FollowUpInstructions    { get; set; }
    public string? AttendingPhysicianNotes { get; set; }

    public DateTime UpdatedAt              { get; set; }
}
