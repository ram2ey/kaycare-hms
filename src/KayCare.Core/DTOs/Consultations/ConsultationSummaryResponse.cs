namespace KayCare.Core.DTOs.Consultations;

public class ConsultationSummaryResponse
{
    public Guid    ConsultationId       { get; set; }
    public Guid    AppointmentId        { get; set; }
    public Guid    PatientId            { get; set; }
    public string  PatientName          { get; set; } = string.Empty;
    public string  MedicalRecordNumber  { get; set; } = string.Empty;
    public Guid    DoctorUserId         { get; set; }
    public string  DoctorName           { get; set; } = string.Empty;
    public string? PrimaryDiagnosisCode { get; set; }
    public string? PrimaryDiagnosisDesc { get; set; }
    public string  Status               { get; set; } = string.Empty;
    public DateTime? SignedAt           { get; set; }
    public DateTime  CreatedAt          { get; set; }
}
