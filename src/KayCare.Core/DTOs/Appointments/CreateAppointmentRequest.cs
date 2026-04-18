using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Appointments;

public class CreateAppointmentRequest
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid DoctorUserId { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; }

    [Range(5, 480)]
    public int DurationMinutes { get; set; } = 30;

    [Required, MaxLength(50)]
    public string AppointmentType { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ChiefComplaint { get; set; }

    [MaxLength(50)]
    public string? Room { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
