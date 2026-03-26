namespace KayCare.Core.DTOs.LabOrders;

public class ManualResultRequest
{
    public string  Result         { get; set; } = string.Empty;
    public string? Notes          { get; set; }
    public string? Unit           { get; set; }           // e.g. "mmol/L" (optional for qualitative tests)
    public string? ReferenceRange { get; set; }           // e.g. "3.9-5.6" (flag auto-computed from this)
}
