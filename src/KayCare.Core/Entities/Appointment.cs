namespace KayCare.Core.Entities;

public class Appointment : TenantEntity
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorUserId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string AppointmentType { get; set; } = string.Empty;
    public string Status { get; set; } = "Scheduled";
    public string? ChiefComplaint { get; set; }
    public string? Room { get; set; }
    public string? Notes { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancellationReason { get; set; }
    public Guid CreatedByUserId { get; set; }

    public Patient Patient { get; set; } = null!;
    public User Doctor { get; set; } = null!;
}
