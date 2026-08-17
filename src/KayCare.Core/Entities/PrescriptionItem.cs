namespace KayCare.Core.Entities;

public class PrescriptionItem
{
    public Guid   ItemId           { get; set; }
    public Guid   TenantId         { get; set; }
    public Guid   PrescriptionId   { get; set; }
    public string MedicationName   { get; set; } = string.Empty;
    public string? GenericName     { get; set; }
    public string Strength         { get; set; } = string.Empty;
    public string DosageForm       { get; set; } = string.Empty;
    public string Frequency        { get; set; } = string.Empty;
    public int    DurationDays     { get; set; }
    public int    Quantity         { get; set; }
    public int    Refills          { get; set; }
    public string? Instructions    { get; set; }
    public bool IsControlledSubstance { get; set; }
    public int  QuantityDispensed    { get; set; }
    public bool IsFullyDispensed     { get; set; }

    // F1.3/F6.1 — best-effort link to the catalog drug matching MedicationName at creation time.
    // Nullable/optional: prescribing a non-stocked or custom-named medication remains allowed;
    // this only lets dispense-time stock deduction match reliably instead of by name string.
    public Guid?          DrugInventoryId { get; set; }
    public DrugInventory? DrugInventory   { get; set; }

    public Prescription Prescription { get; set; } = null!;
}
