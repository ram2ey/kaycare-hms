using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.PrescriptionTemplates;

public class CreatePrescriptionTemplateRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsShared { get; set; }

    [Required, MinLength(1)]
    public List<PrescriptionTemplateItemRequest> Items { get; set; } = [];
}
