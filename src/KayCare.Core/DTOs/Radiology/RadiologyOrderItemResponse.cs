using System;

namespace KayCare.Core.DTOs.Radiology;

public class RadiologyOrderItemResponse
{
    public Guid    RadiologyOrderItemId     { get; set; }
    public Guid    ImagingProcedureId       { get; set; }
    public string  ProcedureName            { get; set; } = string.Empty;
    public string  Modality                 { get; set; } = string.Empty;
    public string  BodyPart                 { get; set; } = string.Empty;
    public string  Department               { get; set; } = string.Empty;
    public int     TatHours                 { get; set; }
    public string? AccessionNumber          { get; set; }
    public string  Status                   { get; set; } = string.Empty;
    public DateTime? AcquiredAt             { get; set; }
    public DateTime? ReportedAt             { get; set; }
    public DateTime? SignedAt               { get; set; }
    public string? Findings                 { get; set; }
    public string? Impression               { get; set; }
    public string? Recommendations          { get; set; }
    public string? ReportingDoctorName      { get; set; }
    public string? PacsViewerUrl            { get; set; }
    public bool    IsTatExceeded            { get; set; }
}
