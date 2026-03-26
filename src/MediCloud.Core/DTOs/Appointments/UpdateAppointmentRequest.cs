using System.ComponentModel.DataAnnotations;

namespace MediCloud.Core.DTOs.Appointments;

public class UpdateAppointmentRequest
{
    public Guid? DoctorUserId { get; set; }
    public DateTime? ScheduledAt { get; set; }

    [Range(5, 480)]
    public int? DurationMinutes { get; set; }

    [MaxLength(50)]
    public string? AppointmentType { get; set; }

    [MaxLength(1000)]
    public string? ChiefComplaint { get; set; }

    [MaxLength(50)]
    public string? Room { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
