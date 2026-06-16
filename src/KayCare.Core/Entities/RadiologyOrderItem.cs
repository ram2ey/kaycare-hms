using System;

namespace KayCare.Core.Entities;

public class RadiologyOrderItem
{
    public Guid   RadiologyOrderItemId { get; set; }
    public Guid   RadiologyOrderId     { get; set; }
    public Guid   TenantId             { get; set; }
    public Guid   ImagingProcedureId   { get; set; }

    public string  ProcedureName     { get; set; } = string.Empty;
    public string  Modality          { get; set; } = string.Empty;
    public string  BodyPart          { get; set; } = string.Empty;
    public string  Department        { get; set; } = "Radiology";
    public int     TatHours          { get; set; }
    public string? AccessionNumber   { get; set; }

    public string    Status          { get; set; } = "Ordered";
    public DateTime? AcquiredAt      { get; set; }
    public DateTime? ReportedAt      { get; set; }
    public DateTime? SignedAt        { get; set; }
    public Guid?     SignedByUserId  { get; set; }

    public string? Findings          { get; set; }
    public string? Impression        { get; set; }
    public string? Recommendations   { get; set; }
    public Guid?   ReportingDoctorUserId { get; set; }

    public string? PacsStudyUid      { get; set; }
    public string? PacsViewerUrl     { get; set; }

    public RadiologyOrder     RadiologyOrder    { get; set; } = null!;
    public ImagingProcedure   ImagingProcedure  { get; set; } = null!;
}
