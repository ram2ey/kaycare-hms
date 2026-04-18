using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Appointments;

public class CancelAppointmentRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}
