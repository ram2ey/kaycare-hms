using System.ComponentModel.DataAnnotations;

namespace MediCloud.Core.DTOs.Prescriptions;

public class PrescriptionItemRequest
{
    [Required, MaxLength(200)] public string  MedicationName        { get; set; } = string.Empty;
    [MaxLength(200)]           public string? GenericName           { get; set; }
    [Required, MaxLength(100)] public string  Strength              { get; set; } = string.Empty;
    [Required, MaxLength(50)]  public string  DosageForm            { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string  Frequency             { get; set; } = string.Empty;

    [Range(1, 365)]  public int DurationDays { get; set; }
    [Range(1, 9999)] public int Quantity     { get; set; }
    [Range(0, 12)]   public int Refills      { get; set; }

    [MaxLength(500)] public string? Instructions       { get; set; }
    public bool IsControlledSubstance { get; set; }
}
