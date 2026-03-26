namespace MediCloud.Core.DTOs.Prescriptions;

public class PrescriptionItemResponse
{
    public Guid    ItemId                { get; set; }
    public string  MedicationName        { get; set; } = string.Empty;
    public string? GenericName           { get; set; }
    public string  Strength              { get; set; } = string.Empty;
    public string  DosageForm            { get; set; } = string.Empty;
    public string  Frequency             { get; set; } = string.Empty;
    public int     DurationDays          { get; set; }
    public int     Quantity              { get; set; }
    public int     Refills               { get; set; }
    public string? Instructions          { get; set; }
    public bool    IsControlledSubstance { get; set; }
}
