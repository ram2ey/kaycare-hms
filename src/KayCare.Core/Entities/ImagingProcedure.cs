using System;

namespace KayCare.Core.Entities;

public class ImagingProcedure
{
    public Guid   ImagingProcedureId { get; set; }
    public string ProcedureCode      { get; set; } = string.Empty;
    public string ProcedureName      { get; set; } = string.Empty;
    public string Modality           { get; set; } = string.Empty;
    public string BodyPart           { get; set; } = string.Empty;
    public string Department         { get; set; } = "Radiology";
    public int    TatHours           { get; set; } = 4;
    public bool   IsActive           { get; set; } = true;
}
