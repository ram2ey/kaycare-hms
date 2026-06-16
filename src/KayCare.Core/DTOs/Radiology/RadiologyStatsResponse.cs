namespace KayCare.Core.DTOs.Radiology;

public class RadiologyStatsResponse
{
    public int ScheduledCount { get; set; }
    public int AcquiredCount { get; set; }
    public int ReportedCount { get; set; }
}
