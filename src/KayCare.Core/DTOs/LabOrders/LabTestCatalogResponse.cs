namespace KayCare.Core.DTOs.LabOrders;

public class LabTestCatalogResponse
{
    public Guid    LabTestCatalogId { get; set; }
    public string  TestCode         { get; set; } = string.Empty;
    public string  TestName         { get; set; } = string.Empty;
    public string  Department       { get; set; } = string.Empty;
    public string? InstrumentType   { get; set; }
    public bool    IsManualEntry         { get; set; }
    public int     TatHours              { get; set; }
    public string? DefaultUnit           { get; set; }
    public string? DefaultReferenceRange { get; set; }
}
