namespace KayCare.Core.DTOs.LabOrders;

public class LabOrderItemResponse
{
    public Guid    LabOrderItemId   { get; set; }
    public Guid    LabTestCatalogId { get; set; }
    public string  TestName         { get; set; } = string.Empty;
    public string  Department       { get; set; } = string.Empty;
    public string? InstrumentType   { get; set; }
    public bool    IsManualEntry    { get; set; }
    public int     TatHours         { get; set; }
    public string? AccessionNumber  { get; set; }
    public string  Status           { get; set; } = string.Empty;
    public DateTime? SampleReceivedAt { get; set; }
    public DateTime? ResultedAt       { get; set; }
    public DateTime? SignedAt         { get; set; }
    public string? ManualResult               { get; set; }
    public string? ManualResultNotes          { get; set; }
    public string? ManualResultUnit           { get; set; }
    public string? ManualResultReferenceRange { get; set; }
    public string? ManualResultFlag           { get; set; }
    public Guid?   LabResultId                { get; set; }
    public bool    IsTatExceeded              { get; set; }
}
