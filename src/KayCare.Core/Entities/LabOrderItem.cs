namespace KayCare.Core.Entities;

/// <summary>
/// One test within a lab order.
/// Does NOT inherit TenantEntity (no timestamps); TenantId set manually in service.
/// </summary>
public class LabOrderItem
{
    public Guid   LabOrderItemId    { get; set; }
    public Guid   LabOrderId        { get; set; }
    public Guid   TenantId          { get; set; }
    public Guid   LabTestCatalogId  { get; set; }

    // Denormalized from catalog so history is preserved if catalog changes
    public string  TestName         { get; set; } = string.Empty;
    public string  Department       { get; set; } = string.Empty;
    public string? InstrumentType   { get; set; }
    public bool    IsManualEntry    { get; set; }
    public int     TatHours         { get; set; }

    /// <summary>Generated when phlebotomist clicks Received. Printed as barcode on sample tube.</summary>
    public string? AccessionNumber  { get; set; }

    public string    Status           { get; set; } = "Ordered"; // Ordered|SampleReceived|Resulted|Signed
    public DateTime? SampleReceivedAt { get; set; }
    public DateTime? ResultedAt       { get; set; }
    public DateTime? SignedAt         { get; set; }
    public Guid?     SignedByUserId   { get; set; }

    // Manual result fields (for malaria, WIDAL, urinalysis, etc.)
    public string? ManualResult           { get; set; }
    public string? ManualResultNotes      { get; set; }
    public string? ManualResultUnit       { get; set; }  // e.g. "mmol/L"
    public string? ManualResultReferenceRange { get; set; }  // e.g. "3.9-5.6"
    public string? ManualResultFlag       { get; set; }  // H | L | N (auto-computed from value vs range)

    /// <summary>Populated by MllpListenerService when HL7 result arrives matching AccessionNumber.</summary>
    public Guid? LabResultId { get; set; }

    // Navigation
    public LabOrder      LabOrder        { get; set; } = null!;
    public LabTestCatalog LabTestCatalog { get; set; } = null!;
    public LabResult?     LabResult      { get; set; }
}
