namespace KayCare.Core.Entities;

public class Consultation : TenantEntity
{
    public Guid ConsultationId { get; set; }
    public Guid AppointmentId  { get; set; }
    public Guid PatientId      { get; set; }
    public Guid DoctorUserId   { get; set; }

    // SOAP notes
    public string? SubjectiveNotes  { get; set; }
    public string? ObjectiveNotes   { get; set; }
    public string? AssessmentNotes  { get; set; }
    public string? PlanNotes        { get; set; }

    // Vitals
    public int?     BloodPressureSystolic  { get; set; }
    public int?     BloodPressureDiastolic { get; set; }
    public int?     HeartRateBPM           { get; set; }
    public decimal? TemperatureCelsius     { get; set; }
    public decimal? WeightKg               { get; set; }
    public decimal? HeightCm               { get; set; }
    public decimal? OxygenSaturationPct    { get; set; }

    // ICD-10 diagnosis
    public string? PrimaryDiagnosisCode { get; set; }
    public string? PrimaryDiagnosisDesc { get; set; }
    public string  SecondaryDiagnoses   { get; set; } = "[]"; // JSON array

    public string    Status   { get; set; } = "Draft";
    public DateTime? SignedAt { get; set; }

    // Navigation
    public Appointment Appointment { get; set; } = null!;
    public Patient     Patient     { get; set; } = null!;
    public User        Doctor      { get; set; } = null!;
}
