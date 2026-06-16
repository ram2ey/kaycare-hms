using System;

namespace KayCare.Core.DTOs.Radiology;

public class ImagingProcedureResponse
{
    public Guid   ImagingProcedureId { get; set; }
    public string ProcedureCode      { get; set; } = string.Empty;
    public string ProcedureName      { get; set; } = string.Empty;
    public string Modality           { get; set; } = string.Empty;
    public string BodyPart           { get; set; } = string.Empty;
    public string Department         { get; set; } = string.Empty;
    public int    TatHours           { get; set; }
    public bool   IsActive           { get; set; }
}
