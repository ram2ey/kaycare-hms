namespace KayCare.Core.Entities;

public class PatientAllergy
{
    public Guid AllergyId { get; set; }
    public Guid TenantId { get; set; }
    public Guid PatientId { get; set; }
    public string AllergyType { get; set; } = string.Empty;   // Drug, Food, Environmental, Other
    public string AllergenName { get; set; } = string.Empty;
    public string? Reaction { get; set; }
    public string Severity { get; set; } = string.Empty;      // Mild, Moderate, Severe, Life-threatening
    public DateTime RecordedAt { get; set; }
    public Guid RecordedByUserId { get; set; }

    public Patient Patient { get; set; } = null!;
}
