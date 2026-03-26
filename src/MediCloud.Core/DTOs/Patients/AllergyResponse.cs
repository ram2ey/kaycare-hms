namespace MediCloud.Core.DTOs.Patients;

public class AllergyResponse
{
    public Guid AllergyId { get; set; }
    public string AllergyType { get; set; } = string.Empty;
    public string AllergenName { get; set; } = string.Empty;
    public string? Reaction { get; set; }
    public string Severity { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}
