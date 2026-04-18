namespace KayCare.Core.DTOs.Appointments;

public class AppointmentDetailResponse : AppointmentResponse
{
    public string? Notes { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
