namespace KayCare.Core.DTOs.Consultations;

public class ConsultationDetailResponse : ConsultationSummaryResponse
{
    // SOAP
    public string? SubjectiveNotes { get; set; }
    public string? ObjectiveNotes  { get; set; }
    public string? AssessmentNotes { get; set; }
    public string? PlanNotes       { get; set; }

    // Vitals
    public int?     BloodPressureSystolic  { get; set; }
    public int?     BloodPressureDiastolic { get; set; }
    public int?     HeartRateBPM           { get; set; }
    public decimal? TemperatureCelsius     { get; set; }
    public decimal? WeightKg               { get; set; }
    public decimal? HeightCm               { get; set; }
    public decimal? OxygenSaturationPct    { get; set; }

    public List<DiagnosisDto> SecondaryDiagnoses { get; set; } = [];
    public DateTime UpdatedAt { get; set; }
}
