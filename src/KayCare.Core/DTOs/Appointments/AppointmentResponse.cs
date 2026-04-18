namespace KayCare.Core.DTOs.Appointments;

public class AppointmentResponse
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public Guid DoctorUserId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string AppointmentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ChiefComplaint { get; set; }
    public string? Room { get; set; }
}
