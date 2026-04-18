namespace KayCare.Core.DTOs.LabResults;

public class LabResultResponse
{
    public Guid    LabResultId          { get; set; }
    public Guid    PatientId            { get; set; }
    public string  PatientMrn           { get; set; } = string.Empty;
    public string  PatientName          { get; set; } = string.Empty;
    public Guid?   OrderingDoctorUserId { get; set; }
    public string? OrderingDoctorName   { get; set; }
    public string  AccessionNumber      { get; set; } = string.Empty;
    public string? OrderCode            { get; set; }
    public string? OrderName            { get; set; }
    public DateTime? OrderedAt          { get; set; }
    public DateTime  ReceivedAt         { get; set; }
    public string    Status             { get; set; } = string.Empty;
    public int       ObservationCount   { get; set; }
    public DateTime  CreatedAt          { get; set; }
}
