using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Patients;

public class AddAllergyRequest
{
    [Required, MaxLength(50)]
    public string AllergyType { get; set; } = string.Empty;   // Drug | Food | Environmental | Other

    [Required, MaxLength(200)]
    public string AllergenName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Reaction { get; set; }

    [Required, MaxLength(20)]
    public string Severity { get; set; } = string.Empty;      // Mild | Moderate | Severe | Life-threatening
}
