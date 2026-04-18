namespace KayCare.Core.Entities;

/// <summary>Global ICD-10 code catalogue — not tenant-scoped.</summary>
public class IcdCode
{
    public int    IcdCodeId   { get; set; } // IDENTITY PK
    public string Code        { get; set; } = string.Empty; // e.g. "A01.0"
    public string Description { get; set; } = string.Empty; // e.g. "Typhoid fever"
    public string Chapter     { get; set; } = string.Empty; // e.g. "Infectious and Parasitic Diseases"
}
