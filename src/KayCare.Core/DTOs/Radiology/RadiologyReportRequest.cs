namespace KayCare.Core.DTOs.Radiology;

public class RadiologyReportRequest
{
    public string  Findings        { get; set; } = string.Empty;
    public string  Impression      { get; set; } = string.Empty;
    public string? Recommendations { get; set; }
    public string? PacsStudyUid    { get; set; }
    public string? PacsViewerUrl   { get; set; }
}
