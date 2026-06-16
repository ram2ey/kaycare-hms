using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Radiology;

public class SaveImagingProcedureRequest
{
    [Required, MaxLength(20)]
    public string ProcedureCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string ProcedureName { get; set; } = string.Empty;

    /// <summary>X-Ray, CT, MRI, Ultrasound, Nuclear Medicine, Fluoroscopy, etc.</summary>
    [Required, MaxLength(100)]
    public string Modality { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string BodyPart { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Department { get; set; } = "Radiology";

    [Range(1, 168)]
    public int TatHours { get; set; } = 4;

    public bool IsActive { get; set; } = true;
}
