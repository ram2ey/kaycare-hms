namespace KayCare.Core.DTOs.Appointments;

public class CalendarRequest
{
    /// <summary>Filter to a specific doctor. Defaults to the authenticated user if they are a Doctor.</summary>
    public Guid? DoctorUserId { get; set; }

    /// <summary>Start of calendar window (inclusive). Defaults to today (UTC).</summary>
    public DateTime? From { get; set; }

    /// <summary>End of calendar window (exclusive). Defaults to From + 7 days.</summary>
    public DateTime? To { get; set; }

    /// <summary>Optional status filter.</summary>
    public string? Status { get; set; }
}
