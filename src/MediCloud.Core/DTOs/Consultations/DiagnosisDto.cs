using System.ComponentModel.DataAnnotations;

namespace MediCloud.Core.DTOs.Consultations;

public class DiagnosisDto
{
    /// <summary>ICD-10 code e.g. "J06.9"</summary>
    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}
