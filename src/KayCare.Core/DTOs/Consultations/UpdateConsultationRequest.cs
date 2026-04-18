using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Consultations;

public class UpdateConsultationRequest
{
    // SOAP
    public string? SubjectiveNotes { get; set; }
    public string? ObjectiveNotes  { get; set; }
    public string? AssessmentNotes { get; set; }
    public string? PlanNotes       { get; set; }

    // Vitals
    [Range(40,  300)] public int?     BloodPressureSystolic  { get; set; }
    [Range(20,  200)] public int?     BloodPressureDiastolic { get; set; }
    [Range(20,  300)] public int?     HeartRateBPM           { get; set; }
    [Range(30,   45)] public decimal? TemperatureCelsius     { get; set; }
    [Range(0.5, 500)] public decimal? WeightKg               { get; set; }
    [Range(20,  300)] public decimal? HeightCm               { get; set; }
    [Range(50,  100)] public decimal? OxygenSaturationPct    { get; set; }

    // ICD-10
    [MaxLength(20)]  public string? PrimaryDiagnosisCode { get; set; }
    [MaxLength(500)] public string? PrimaryDiagnosisDesc { get; set; }

    public List<DiagnosisDto>? SecondaryDiagnoses { get; set; }
}
