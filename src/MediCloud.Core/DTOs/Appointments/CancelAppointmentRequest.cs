using System.ComponentModel.DataAnnotations;

namespace MediCloud.Core.DTOs.Appointments;

public class CancelAppointmentRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}
