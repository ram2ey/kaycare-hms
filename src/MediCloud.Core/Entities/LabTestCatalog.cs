namespace MediCloud.Core.Entities;

/// <summary>
/// Global catalog of available lab tests — not tenant-scoped.
/// Seeded with common Ghanaian clinical lab tests.
/// </summary>
public class LabTestCatalog
{
    public Guid   LabTestCatalogId { get; set; }
    public string TestCode         { get; set; } = string.Empty; // e.g. "FBC"
    public string TestName         { get; set; } = string.Empty; // e.g. "Full Blood Count"
    public string Department       { get; set; } = string.Empty; // Haematology, Chemistry, etc.
    public string? InstrumentType  { get; set; }                 // DxH560 | DxC500 | CobasE411 | null(manual)
    public bool    IsManualEntry        { get; set; }
    public int     TatHours             { get; set; } = 4;        // expected turnaround hours
    public bool    IsActive             { get; set; } = true;

    /// <summary>Default unit shown to lab technician on manual entry (e.g. "mmol/L").</summary>
    public string? DefaultUnit          { get; set; }
    /// <summary>Default reference range shown to lab technician (e.g. "3.9-5.6").</summary>
    public string? DefaultReferenceRange { get; set; }
}
